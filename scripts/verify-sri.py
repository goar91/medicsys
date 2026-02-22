#!/usr/bin/env python3
"""Verificación completa del flujo SRI en modo pruebas."""
import json, sys, urllib.request, ssl

BASE = "http://localhost:5154/api"
ctx = ssl._create_unverified_context()

def post(url, data=None, token=None):
    headers = {"Content-Type": "application/json"}
    if token:
        headers["Authorization"] = f"Bearer {token}"
    body = json.dumps(data).encode() if data else None
    req = urllib.request.Request(url, data=body, headers=headers, method="POST")
    with urllib.request.urlopen(req) as r:
        return json.loads(r.read())

def get(url, token=None):
    headers = {}
    if token:
        headers["Authorization"] = f"Bearer {token}"
    req = urllib.request.Request(url, headers=headers)
    with urllib.request.urlopen(req) as r:
        return json.loads(r.read())

print("=" * 60)
print("  VERIFICACIÓN SRI - MODO PRUEBAS")
print("=" * 60)

# 1. Login
print("\n1. Autenticación...")
auth = post(f"{BASE}/auth/login", {
    "email": "odontologo@medicsys.com",
    "password": "Odontologo123!"
})
token = auth["token"]
print(f"   ✅ Login exitoso: {auth['user']['fullName']} ({auth['user']['role']})")

# 2. Obtener config facturación
print("\n2. Configuración de facturación...")
try:
    config = get(f"{BASE}/invoices/config", token)
    print(f"   Establecimiento:   {config['establishmentCode']}")
    print(f"   Punto de emisión:  {config['emissionPoint']}")
    print(f"   Próximo secuencial:{config['nextSequential']}")
    print(f"   Próximo número:    {config['nextNumber']}")
except Exception as e:
    print(f"   ⚠️  No se pudo obtener config: {e}")

# 3. Crear factura de prueba
print("\n3. Creando factura de prueba (ambiente: Pruebas, sendToSri: true)...")
invoice = post(f"{BASE}/invoices", {
    "customerIdentificationType": "05",
    "customerIdentification": "0102345678",
    "customerName": "PACIENTE PRUEBA SRI",
    "customerAddress": "Cuenca, Ecuador",
    "customerPhone": "0999999999",
    "customerEmail": "prueba@test.com",
    "observations": "Factura de verificación SRI modo pruebas",
    "paymentMethod": "Cash",
    "sriEnvironment": "Pruebas",
    "sendToSri": True,
    "items": [
        {
            "description": "Consulta General - Verificación SRI",
            "quantity": 1,
            "unitPrice": 35.00,
            "discountPercent": 0
        }
    ]
}, token)

print(f"   ✅ Factura creada exitosamente")

# 4. Análisis de resultado
print("\n" + "=" * 60)
print("  RESULTADO DE LA FACTURA")
print("=" * 60)
print(f"   Número:              {invoice['number']}")
print(f"   Secuencial:          {invoice['sequential']}")
print(f"   Estado:              {invoice['status']}")
print(f"   Ambiente SRI:        {invoice['sriEnvironment']}")
print(f"   Subtotal:            ${invoice['subtotal']:.2f}")
print(f"   IVA (15%):           ${invoice['tax']:.2f}")
print(f"   Total:               ${invoice['total']:.2f}")
print(f"   Total a cobrar:      ${invoice['totalToCharge']:.2f}")

clave = invoice.get("sriAccessKey", "")
auth_num = invoice.get("sriAuthorizationNumber", "")
fecha_auth = invoice.get("sriAuthorizedAt", "N/A")
mensajes = invoice.get("sriMessages", None)

print(f"\n   Clave de Acceso SRI:")
print(f"     {clave}")
print(f"\n   Número de Autorización SRI:")
print(f"     {auth_num}")
print(f"\n   Fecha de Autorización:  {fecha_auth}")
print(f"   Mensajes SRI:          {mensajes or 'Ninguno'}")

# 5. Validaciones
print("\n" + "=" * 60)
print("  VALIDACIONES")
print("=" * 60)

errors = []
warnings = []

# 5.1 Estado
if invoice["status"] == "Authorized":
    print("   ✅ Estado: AUTORIZADO")
else:
    errors.append(f"Estado inesperado: {invoice['status']}")
    print(f"   ❌ Estado: {invoice['status']} (se esperaba 'Authorized')")

# 5.2 Ambiente
if invoice["sriEnvironment"] == "Pruebas":
    print("   ✅ Ambiente: PRUEBAS (correcto)")
else:
    errors.append(f"Ambiente: {invoice['sriEnvironment']}")
    print(f"   ❌ Ambiente: {invoice['sriEnvironment']} (se esperaba 'Pruebas')")

# 5.3 Clave de acceso
if clave and len(clave) == 49:
    print(f"   ✅ Clave de acceso: {len(clave)} dígitos (correcto)")
else:
    errors.append(f"Clave de acceso inválida: {len(clave)} dígitos")
    print(f"   ❌ Clave de acceso: {len(clave)} dígitos (se esperan 49)")

# 5.4 Autorización == Clave de acceso
if clave == auth_num:
    print("   ✅ Nro. Autorización = Clave de acceso (correcto)")
else:
    errors.append("Nro. Autorización != Clave de acceso")
    print("   ❌ Nro. Autorización != Clave de acceso")

# 5.5 Desglose clave
if clave and len(clave) == 49:
    fecha = clave[0:8]
    tipo_doc = clave[8:10]
    ruc = clave[10:23]
    ambiente_codigo = clave[23:24]
    estab = clave[24:27]
    pto_emi = clave[27:30]
    seq = clave[30:39]
    codigo_num = clave[39:47]
    tipo_emision = clave[47:48]
    digito_verif = clave[48:49]

    print(f"\n   --- Desglose de Clave de Acceso ---")
    print(f"   Fecha emisión:       {fecha[0:2]}/{fecha[2:4]}/{fecha[4:8]}")
    
    if tipo_doc == "01":
        print(f"   Tipo comprobante:    {tipo_doc} (Factura) ✅")
    else:
        print(f"   Tipo comprobante:    {tipo_doc} ❌ (se esperaba 01)")
        errors.append(f"Tipo comprobante: {tipo_doc}")

    print(f"   RUC emisor:          {ruc}")

    if ambiente_codigo == "1":
        print(f"   Ambiente:            {ambiente_codigo} (Pruebas) ✅")
    elif ambiente_codigo == "2":
        warnings.append("Ambiente en clave = Producción")
        print(f"   Ambiente:            {ambiente_codigo} (Producción) ⚠️")
    else:
        errors.append(f"Código ambiente inválido: {ambiente_codigo}")
        print(f"   Ambiente:            {ambiente_codigo} ❌")

    print(f"   Establecimiento:     {estab}")
    print(f"   Punto de emisión:    {pto_emi}")
    print(f"   Secuencial:          {seq} (= {int(seq)})")
    print(f"   Código numérico:     {codigo_num}")
    
    if tipo_emision == "1":
        print(f"   Tipo emisión:        {tipo_emision} (Normal) ✅")
    else:
        print(f"   Tipo emisión:        {tipo_emision} ❌")
        errors.append(f"Tipo emisión: {tipo_emision}")

    print(f"   Dígito verificador:  {digito_verif}")

# 5.6 Fecha autorización
if fecha_auth and fecha_auth != "N/A":
    print(f"\n   ✅ Tiene fecha de autorización: {fecha_auth}")
else:
    errors.append("Sin fecha de autorización")
    print(f"\n   ❌ Sin fecha de autorización")

# Resumen
print("\n" + "=" * 60)
if errors:
    print(f"  ❌ VERIFICACIÓN FALLIDA - {len(errors)} error(es):")
    for e in errors:
        print(f"     - {e}")
else:
    print("  ✅ VERIFICACIÓN EXITOSA - Facturación SRI en modo pruebas OK")
    if warnings:
        for w in warnings:
            print(f"     ⚠️  {w}")
print("=" * 60)
