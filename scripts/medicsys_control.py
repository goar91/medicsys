#!/usr/bin/env python3
import json
import platform
import subprocess
import threading
import time
import urllib.error
import urllib.request
import webbrowser
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
APP_URL = "http://localhost:4200"
CONTROL_HOST = "127.0.0.1"
CONTROL_PORT = 8765


HTML = """<!doctype html>
<html lang="es">
<head>
  <meta charset="utf-8"/>
  <meta name="viewport" content="width=device-width,initial-scale=1"/>
  <title>MEDICSYS Control</title>
  <style>
    body { font-family: -apple-system, Segoe UI, Roboto, sans-serif; margin: 24px; background: #f3f6fb; color: #0f172a; }
    .wrap { max-width: 900px; margin: 0 auto; }
    .row { display: flex; gap: 12px; margin-bottom: 16px; }
    button { padding: 12px 18px; border: 0; border-radius: 10px; font-size: 16px; cursor: pointer; }
    #start { background: #0f766e; color: white; }
    #stop { background: #b91c1c; color: white; }
    .card { background: white; border-radius: 12px; padding: 14px; box-shadow: 0 2px 10px rgba(15,23,42,.08); }
    #state { font-weight: 700; }
    pre { background: #0b1020; color: #d1e4ff; padding: 12px; border-radius: 10px; height: 420px; overflow: auto; }
  </style>
</head>
<body>
  <div class="wrap">
    <h1>MEDICSYS Control</h1>
    <p>Orden solicitado: <b>1) Backend</b>, <b>2) Base de datos</b>, <b>3) Frontend</b>.</p>
    <div class="row">
      <button id="start">Ejecutar Todo</button>
      <button id="stop">Detener Todo</button>
      <div class="card">Estado: <span id="state">-</span></div>
    </div>
    <div class="card">
      <pre id="logs"></pre>
    </div>
  </div>
  <script>
    async function post(path) {
      const r = await fetch(path, { method: "POST" });
      return r.json();
    }
    async function refresh() {
      const r = await fetch("/status");
      const s = await r.json();
      document.getElementById("state").textContent = s.running ? "Ejecutando..." : "Disponible";
      document.getElementById("logs").textContent = s.logs.join("\\n");
    }
    document.getElementById("start").onclick = async () => { await post("/start"); await refresh(); };
    document.getElementById("stop").onclick = async () => { await post("/stop"); await refresh(); };
    setInterval(refresh, 1500);
    refresh();
  </script>
</body>
</html>
"""


class State:
    def __init__(self):
        self.running = False
        self.logs = []
        self.lock = threading.Lock()

    def log(self, message):
        ts = time.strftime("%H:%M:%S")
        line = f"[{ts}] {message}"
        with self.lock:
            self.logs.append(line)
            self.logs = self.logs[-350:]
        print(line, flush=True)

    def set_running(self, value):
        with self.lock:
            self.running = value

    def snapshot(self):
        with self.lock:
            return {"running": self.running, "logs": list(self.logs)}


STATE = State()


def run_cmd(cmd):
    return subprocess.run(cmd, cwd=str(ROOT), text=True, capture_output=True)


def must_run(cmd):
    proc = run_cmd(cmd)
    if proc.returncode != 0:
        msg = proc.stderr.strip() or proc.stdout.strip() or f"error ejecutando: {' '.join(cmd)}"
        raise RuntimeError(msg)
    out = proc.stdout.strip()
    if out:
        STATE.log(out)
    return proc


def wait_postgres_healthy(timeout=120):
    STATE.log("Esperando base de datos en estado healthy...")
    end = time.time() + timeout
    while time.time() < end:
        proc = run_cmd(["docker", "inspect", "-f", "{{.State.Health.Status}}", "medicsys-postgres"])
        if proc.returncode == 0 and proc.stdout.strip() == "healthy":
            STATE.log("Base de datos healthy.")
            return
        time.sleep(2)
    raise RuntimeError("Timeout esperando PostgreSQL healthy.")


def wait_frontend(timeout=180):
    STATE.log("Esperando frontend en http://localhost:4200 ...")
    end = time.time() + timeout
    while time.time() < end:
        try:
            with urllib.request.urlopen(APP_URL, timeout=8) as resp:
                if resp.status == 200:
                    STATE.log("Frontend disponible.")
                    return
        except Exception:
            pass
        time.sleep(3)
    raise RuntimeError("Timeout esperando frontend disponible.")


def open_browser(url):
    STATE.log(f"Abriendo navegador: {url}")
    webbrowser.open(url, new=2)


def close_browser():
    STATE.log("Cerrando navegador en el sistema operativo...")
    system = platform.system()
    if system == "Darwin":
        for app in ("Google Chrome", "Safari", "Firefox", "Microsoft Edge"):
            run_cmd(["osascript", "-e", f'tell application "{app}" to if it is running then quit'])
    elif system == "Windows":
        for exe in ("chrome.exe", "msedge.exe", "firefox.exe", "brave.exe", "opera.exe"):
            run_cmd(["taskkill", "/F", "/IM", exe])
    else:
        run_cmd(["pkill", "-f", "chrome"])
        run_cmd(["pkill", "-f", "firefox"])
        run_cmd(["pkill", "-f", "edge"])


def start_all():
    if STATE.snapshot()["running"]:
        return
    STATE.set_running(True)
    try:
        STATE.log("Inicio solicitado (orden: backend -> base de datos -> frontend).")
        run_cmd(["docker", "network", "create", "medicsys-net"])

        STATE.log("1) Iniciando backend...")
        must_run(["docker", "compose", "up", "-d", "--no-deps", "api"])

        STATE.log("2) Iniciando base de datos...")
        must_run(["docker", "compose", "up", "-d", "--no-deps", "postgres"])
        wait_postgres_healthy()

        STATE.log("3) Iniciando frontend...")
        must_run(["docker", "compose", "up", "-d", "--no-deps", "web"])
        wait_frontend()

        open_browser(APP_URL)
        STATE.log("Sistema iniciado correctamente.")
    except Exception as ex:
        STATE.log(f"ERROR: {ex}")
    finally:
        STATE.set_running(False)


def stop_all():
    if STATE.snapshot()["running"]:
        return
    STATE.set_running(True)
    try:
        STATE.log("Detención solicitada (orden: backend -> base de datos -> frontend).")

        STATE.log("1) Deteniendo backend...")
        must_run(["docker", "compose", "stop", "api"])

        STATE.log("2) Deteniendo base de datos...")
        must_run(["docker", "compose", "stop", "postgres"])

        STATE.log("3) Deteniendo frontend...")
        must_run(["docker", "compose", "stop", "web"])

        close_browser()
        STATE.log("Sistema detenido correctamente.")
    except Exception as ex:
        STATE.log(f"ERROR: {ex}")
    finally:
        STATE.set_running(False)


class Handler(BaseHTTPRequestHandler):
    def _json(self, payload, status=200):
        data = json.dumps(payload, ensure_ascii=False).encode("utf-8")
        self.send_response(status)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(data)))
        self.end_headers()
        self.wfile.write(data)

    def _html(self, html):
        data = html.encode("utf-8")
        self.send_response(200)
        self.send_header("Content-Type", "text/html; charset=utf-8")
        self.send_header("Content-Length", str(len(data)))
        self.end_headers()
        self.wfile.write(data)

    def do_GET(self):
        if self.path == "/":
            return self._html(HTML)
        if self.path == "/status":
            return self._json(STATE.snapshot())
        return self._json({"error": "not_found"}, status=404)

    def do_POST(self):
        if self.path == "/start":
            threading.Thread(target=start_all, daemon=True).start()
            return self._json({"ok": True})
        if self.path == "/stop":
            threading.Thread(target=stop_all, daemon=True).start()
            return self._json({"ok": True})
        return self._json({"error": "not_found"}, status=404)

    def log_message(self, fmt, *args):
        return


def main():
    STATE.log(f"Proyecto: {ROOT}")
    STATE.log(f"Panel en: http://{CONTROL_HOST}:{CONTROL_PORT}")
    server = ThreadingHTTPServer((CONTROL_HOST, CONTROL_PORT), Handler)
    open_browser(f"http://{CONTROL_HOST}:{CONTROL_PORT}")
    server.serve_forever()


if __name__ == "__main__":
    main()
