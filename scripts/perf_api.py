#!/usr/bin/env python3
import argparse
import concurrent.futures
import csv
import json
import statistics
import time
import urllib.error
import urllib.parse
import urllib.request
from dataclasses import dataclass
from datetime import datetime, timedelta, timezone
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = ROOT / "artifacts" / "perf"
OUT_DIR.mkdir(parents=True, exist_ok=True)


LOGIN_CANDIDATES = [
    ("odontologo@medicsys.com", "Odontologo123!"),
    ("odontologo1@medicsys.com", "Odontologo123!"),
    ("profesor@medicsys.com", "Profesor123!"),
    ("profesor@medicsys.local", "Medicsys#2026"),
]


@dataclass
class Sample:
    ok: bool
    status: int
    ms: float
    error: str = ""


def request_json(method, url, headers=None, body=None, timeout=45):
    headers = headers or {}
    data = None
    if body is not None:
        data = json.dumps(body).encode("utf-8")
        headers = {**headers, "Content-Type": "application/json"}
    req = urllib.request.Request(url=url, method=method, headers=headers, data=data)
    with urllib.request.urlopen(req, timeout=timeout) as resp:
        payload = resp.read().decode("utf-8")
        return resp.status, json.loads(payload) if payload else {}


def request_raw(method, url, headers=None, body=None, timeout=45):
    headers = headers or {}
    data = None
    if body is not None:
        data = json.dumps(body).encode("utf-8")
        headers = {**headers, "Content-Type": "application/json"}
    req = urllib.request.Request(url=url, method=method, headers=headers, data=data)
    with urllib.request.urlopen(req, timeout=timeout) as resp:
        _ = resp.read()
        return resp.status


def health_check(base_url):
    status = request_raw("GET", f"{base_url}/health", timeout=15)
    if status != 200:
        raise RuntimeError(f"Health check falló: {status}")


def login(base_url):
    for email, password in LOGIN_CANDIDATES:
        try:
            status, payload = request_json(
                "POST",
                f"{base_url}/api/auth/login",
                body={"email": email, "password": password},
                timeout=20,
            )
            token = payload.get("token")
            if status == 200 and token:
                return email, password, token
        except Exception:
            continue
    raise RuntimeError("No se pudo autenticar con las credenciales conocidas.")


def percentile(values, p):
    if not values:
        return 0.0
    ordered = sorted(values)
    k = int(round((p / 100.0) * (len(ordered) - 1)))
    return round(float(ordered[k]), 2)


def run_one(method, url, headers=None, body=None):
    started = time.perf_counter()
    try:
        status = request_raw(method, url, headers=headers, body=body, timeout=60)
        elapsed_ms = (time.perf_counter() - started) * 1000.0
        return Sample(ok=200 <= status < 300, status=status, ms=elapsed_ms)
    except urllib.error.HTTPError as ex:
        elapsed_ms = (time.perf_counter() - started) * 1000.0
        return Sample(ok=False, status=ex.code, ms=elapsed_ms, error=str(ex))
    except Exception as ex:
        elapsed_ms = (time.perf_counter() - started) * 1000.0
        return Sample(ok=False, status=0, ms=elapsed_ms, error=str(ex))


def run_scenario(name, method, url, headers, body, requests, concurrency, warmup):
    for _ in range(warmup):
        run_one(method, url, headers=headers, body=body)

    started = time.perf_counter()
    samples = []
    with concurrent.futures.ThreadPoolExecutor(max_workers=concurrency) as ex:
        futures = [
            ex.submit(run_one, method, url, headers, body)
            for _ in range(requests)
        ]
        for f in concurrent.futures.as_completed(futures):
            samples.append(f.result())
    elapsed = max(time.perf_counter() - started, 0.001)

    lat = [s.ms for s in samples]
    success = sum(1 for s in samples if s.ok)
    errors = requests - success
    status_counts = {}
    for s in samples:
        status_counts[s.status] = status_counts.get(s.status, 0) + 1

    return {
        "scenario": name,
        "method": method,
        "requests": requests,
        "concurrency": concurrency,
        "success": success,
        "errors": errors,
        "error_rate_pct": round((errors / requests) * 100.0, 2),
        "rps": round(requests / elapsed, 2),
        "avg_ms": round(statistics.fmean(lat), 2) if lat else 0.0,
        "p50_ms": percentile(lat, 50),
        "p95_ms": percentile(lat, 95),
        "p99_ms": percentile(lat, 99),
        "min_ms": round(min(lat), 2) if lat else 0.0,
        "max_ms": round(max(lat), 2) if lat else 0.0,
        "statuses": ", ".join(f"{k}:{v}" for k, v in sorted(status_counts.items())),
    }


def main():
    parser = argparse.ArgumentParser(description="MEDICSYS API performance test")
    parser.add_argument("--base-url", default="http://localhost:5154")
    parser.add_argument("--requests", type=int, default=40)
    parser.add_argument("--concurrency", type=int, default=8)
    parser.add_argument("--warmup", type=int, default=3)
    args = parser.parse_args()

    base = args.base_url.rstrip("/")
    print(f"[INFO] Base URL: {base}")
    health_check(base)
    user, user_password, token = login(base)
    print(f"[INFO] Autenticado como: {user}")
    headers = {"Authorization": f"Bearer {token}"}

    today = datetime.now(timezone.utc).date()
    from_date = (today - timedelta(days=150)).isoformat()
    to_date = (today + timedelta(days=1)).isoformat()
    q = urllib.parse.urlencode({"startDate": from_date, "endDate": to_date})
    q_comp = urllib.parse.urlencode({"dateFrom": from_date, "dateTo": to_date})
    q_acc = urllib.parse.urlencode({"from": from_date, "to": to_date})

    scenarios = [
        ("Login", "POST", f"{base}/api/auth/login", {}, {"email": user, "password": user_password}),
        ("Historias clinicas", "GET", f"{base}/api/clinical-histories", headers, None),
        ("Gastos summary", "GET", f"{base}/api/odontologia/gastos/summary", headers, None),
        ("Gastos 5 meses", "GET", f"{base}/api/odontologia/gastos?{q}", headers, None),
        ("Compras 5 meses", "GET", f"{base}/api/odontologia/compras?{q_comp}", headers, None),
        ("Contabilidad 5 meses", "GET", f"{base}/api/accounting/summary?{q_acc}", headers, None),
        ("Reporte financiero", "GET", f"{base}/api/odontologia/reportes/financiero?{q}", headers, None),
    ]

    results = []
    for name, method, url, hdrs, body in scenarios:
        print(f"[RUN] {name} -> {method} {url}")
        results.append(
            run_scenario(
                name=name,
                method=method,
                url=url,
                headers=hdrs,
                body=body,
                requests=args.requests,
                concurrency=args.concurrency,
                warmup=args.warmup,
            )
        )

    stamp = datetime.now().strftime("%Y%m%d-%H%M%S")
    json_path = OUT_DIR / f"api-performance-{stamp}.json"
    csv_path = OUT_DIR / f"api-performance-{stamp}.csv"

    with json_path.open("w", encoding="utf-8") as f:
        json.dump(results, f, ensure_ascii=False, indent=2)

    with csv_path.open("w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(f, fieldnames=list(results[0].keys()))
        writer.writeheader()
        writer.writerows(results)

    print("\n=== RESUMEN ===")
    for r in results:
        print(
            f"{r['scenario']}: avg={r['avg_ms']}ms p95={r['p95_ms']}ms "
            f"rps={r['rps']} err={r['error_rate_pct']}%"
        )
    print(f"\n[OK] JSON: {json_path}")
    print(f"[OK] CSV : {csv_path}")


if __name__ == "__main__":
    main()
