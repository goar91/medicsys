using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;

namespace MEDICSYS.Api.Services;

/// <summary>
/// Firma documentos XML con XAdES-BES según las especificaciones del SRI Ecuador.
///
/// IMPLEMENTACIÓN MANUAL — sin usar System.Security.Cryptography.Xml.SignedXml.
///
/// PROBLEMA ANTERIOR: SignedXml genera &lt;Signature xmlns="...dsig#"&gt; usando el
/// namespace por defecto. Ese namespace queda VISIBLE en el subtree
/// &lt;etsi:SignedProperties&gt;; el validador SRI lo incluye en la C14N de ese subtree
/// pero nuestro cómputo standalone no lo incluye → digests distintos
/// → error [39] FIRMA INVALIDA.
///
/// SOLUCIÓN: usar el prefijo "ds:" explícito en todos los elementos dsig.
/// Los únicos namespaces visibles en &lt;etsi:SignedProperties&gt; en el documento
/// final serán xmlns:ds (de &lt;ds:Signature&gt;) y xmlns:etsi (de
/// &lt;etsi:QualifyingProperties&gt;), que coincide con el documento autónomo que
/// usamos para calcular su digest. ✓
/// </summary>
public static class SriXadesSigner
{
    private const string XadesNs   = "http://uri.etsi.org/01903/v1.3.2#";
    private const string DsigNs    = "http://www.w3.org/2000/09/xmldsig#";
    private const string C14NUrl   = "http://www.w3.org/TR/2001/REC-xml-c14n-20010315";
    private const string RsaSha1   = "http://www.w3.org/2000/09/xmldsig#rsa-sha1";
    private const string Sha1Url   = "http://www.w3.org/2000/09/xmldsig#sha1";
    private const string EnvSigUrl = "http://www.w3.org/2000/09/xmldsig#enveloped-signature";
    private const string SpType    = "http://uri.etsi.org/01903#SignedProperties";

    /// <summary>
    /// Firma el documento XML con XAdES-BES usando el certificado .p12 indicado.
    /// Retorna el XML firmado listo para enviar al SRI.
    /// </summary>
    public static string SignXml(string xmlContent, string certificatePath, string certificatePassword)
    {
        // 1. Cargar certificado
        var cert = X509CertificateLoader.LoadPkcs12FromFile(
            certificatePath, certificatePassword,
            X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet);

        using var rsaKey = cert.GetRSAPrivateKey()
            ?? throw new InvalidOperationException(
                "El certificado no contiene clave privada RSA válida.");

        // 2. IDs únicos
        var uid       = Guid.NewGuid().ToString("N")[..8];
        var sigId     = "Signature"     + uid;
        var sigValId  = "SignatureValue" + uid;
        var keyInfoId = "Certificate"   + uid;
        var objectId  = sigId + "-Object0";
        var spId      = sigId + "-SignedPropertiesID0";
        var refDocId  = "Reference-"   + uid;

        // 3. Datos del certificado (SHA-1 requerido por SRI Ecuador)
#pragma warning disable CA5350
        var certDer    = cert.RawData;
        var certB64    = Convert.ToBase64String(certDer);
        var certDigest = Convert.ToBase64String(SHA1.HashData(certDer));
#pragma warning restore CA5350

        var issuerName = cert.IssuerName.Name ?? string.Empty;
        var serialDec  = BigInteger
            .Parse("0" + cert.SerialNumber, NumberStyles.HexNumber)
            .ToString(CultureInfo.InvariantCulture);
        // El validador del SRI Ecuador no maneja bien offsets de zona horaria en
        // etsi:SigningTime. Usar la hora de Ecuador (UTC-5) sin offset explícito
        // evita el error [39] "La fecha contenida en la firma es posterior a la actual".
        var signingTime = DateTimeHelper.Now()
            .ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);

        // 4. Clave pública RSA para KeyInfo
        var rsaParams = rsaKey.ExportParameters(includePrivateParameters: false);
        var modB64    = Convert.ToBase64String(rsaParams.Modulus!);
        var expB64    = Convert.ToBase64String(rsaParams.Exponent!);

        // PASO A: Digest del comprobante (Reference URI="" + enveloped-sig)
        // El documento aún NO tiene Signature → enveloped-transform es no-op.
        // PreserveWhitespace=false → XML compacto; igual que lo que se envía al SRI.
        var xmlDoc = new XmlDocument { PreserveWhitespace = false };
        xmlDoc.LoadXml(xmlContent);

        var docDigest = C14nSha1(xmlDoc);

        // PASO B: Digest de etsi:SignedProperties como documento autónomo
        // xmlns:etsi y xmlns:ds declarados en el ELEMENTO RAÍZ para que el C14N
        // autónomo coincida con el C14N del subtree en el documento final.
        // Demostración: en el doc final, namespaces visibles en etsi:SignedProperties:
        //   xmlns:ds   de ds:Signature (ancestro)
        //   xmlns:etsi de etsi:QualifyingProperties (padre)
        // C14N subtree renderiza todos los namespaces visibles sobre el elemento raíz
        // → produce el mismo resultado que el doc autónomo con esos xmlns en el root. ✓
        var spXml    = BuildSignedPropertiesXml(spId, signingTime, certDigest,
                                                issuerName, serialDec, refDocId);
        var spDigest = C14nSha1(spXml);

        // PASO C: ds:SignedInfo con los dos digests
        // Prefijo "ds:" explícito — NO namespace por defecto — para que xmlns:ds
        // no se propague como xmlns="" a los elementos etsi:*.
        // URI="#comprobante" apunta explícitamente al elemento raíz <factura id="comprobante">
        // El SRI verifica que el nodo [comprobante] esté firmado; URI="" (documento completo)
        // puede no satisfacer esa comprobación específica del validador ecuatoriano.
        var signedInfoXml =
            $"<ds:SignedInfo xmlns:ds=\"{DsigNs}\">" +
                $"<ds:CanonicalizationMethod Algorithm=\"{C14NUrl}\"/>" +
                $"<ds:SignatureMethod Algorithm=\"{RsaSha1}\"/>" +
                $"<ds:Reference Id=\"{refDocId}\" URI=\"#comprobante\">" +
                    $"<ds:Transforms>" +
                        $"<ds:Transform Algorithm=\"{EnvSigUrl}\"/>" +
                    $"</ds:Transforms>" +
                    $"<ds:DigestMethod Algorithm=\"{Sha1Url}\"/>" +
                    $"<ds:DigestValue>{docDigest}</ds:DigestValue>" +
                $"</ds:Reference>" +
                $"<ds:Reference Type=\"{SpType}\" URI=\"#{spId}\">" +
                    $"<ds:DigestMethod Algorithm=\"{Sha1Url}\"/>" +
                    $"<ds:DigestValue>{spDigest}</ds:DigestValue>" +
                $"</ds:Reference>" +
            $"</ds:SignedInfo>";

        // PASO D: Firma RSA-SHA1 del SignedInfo canonicalizado
        var sigValueB64 = C14nRsaSha1(signedInfoXml, rsaKey);

        // PASO E: Ensamblar ds:Signature completo (todo en una línea → sin whitespace extra)
        var signatureXml =
            $"<ds:Signature xmlns:ds=\"{DsigNs}\" Id=\"{sigId}\">" +
                signedInfoXml +
                $"<ds:SignatureValue Id=\"{sigValId}\">{sigValueB64}</ds:SignatureValue>" +
                $"<ds:KeyInfo Id=\"{keyInfoId}\">" +
                    $"<ds:X509Data>" +
                        $"<ds:X509Certificate>{certB64}</ds:X509Certificate>" +
                    $"</ds:X509Data>" +
                    $"<ds:KeyValue>" +
                        $"<ds:RSAKeyValue>" +
                            $"<ds:Modulus>{modB64}</ds:Modulus>" +
                            $"<ds:Exponent>{expB64}</ds:Exponent>" +
                        $"</ds:RSAKeyValue>" +
                    $"</ds:KeyValue>" +
                $"</ds:KeyInfo>" +
                $"<ds:Object Id=\"{objectId}\">" +
                    $"<etsi:QualifyingProperties xmlns:etsi=\"{XadesNs}\" Target=\"#{sigId}\">" +
                        spXml +
                    $"</etsi:QualifyingProperties>" +
                $"</ds:Object>" +
            $"</ds:Signature>";

        // PASO F: Importar al documento y retornar
        var sigDoc = new XmlDocument { PreserveWhitespace = false };
        sigDoc.LoadXml(signatureXml);
        var importedSig = xmlDoc.ImportNode(sigDoc.DocumentElement!, deep: true);
        xmlDoc.DocumentElement!.AppendChild(importedSig);
        return xmlDoc.OuterXml;
    }

    /// <summary>
    /// Construye etsi:SignedProperties como XML compacto con xmlns:etsi Y xmlns:ds
    /// declarados en el elemento raíz. Garantiza que el C14N autónomo coincida con
    /// el C14N del subtree en el documento final (ver comentario de clase). ✓
    /// </summary>
    private static string BuildSignedPropertiesXml(
        string spId, string signingTime, string certDigest,
        string issuerName, string serialDec, string refDocId) =>
        $"<etsi:SignedProperties xmlns:etsi=\"{XadesNs}\" xmlns:ds=\"{DsigNs}\" Id=\"{spId}\">" +
            $"<etsi:SignedSignatureProperties>" +
                $"<etsi:SigningTime>{signingTime}</etsi:SigningTime>" +
                $"<etsi:SigningCertificate>" +
                    $"<etsi:Cert>" +
                        $"<etsi:CertDigest>" +
                            $"<ds:DigestMethod Algorithm=\"{Sha1Url}\"/>" +
                            $"<ds:DigestValue>{certDigest}</ds:DigestValue>" +
                        $"</etsi:CertDigest>" +
                        $"<etsi:IssuerSerial>" +
                            $"<ds:X509IssuerName>{EscapeXml(issuerName)}</ds:X509IssuerName>" +
                            $"<ds:X509SerialNumber>{serialDec}</ds:X509SerialNumber>" +
                        $"</etsi:IssuerSerial>" +
                    $"</etsi:Cert>" +
                $"</etsi:SigningCertificate>" +
            $"</etsi:SignedSignatureProperties>" +
            $"<etsi:SignedDataObjectProperties>" +
                $"<etsi:DataObjectFormat ObjectReference=\"#{refDocId}\">" +
                    $"<etsi:Description>contenido comprobante</etsi:Description>" +
                    $"<etsi:MimeType>text/xml</etsi:MimeType>" +
                $"</etsi:DataObjectFormat>" +
            $"</etsi:SignedDataObjectProperties>" +
        $"</etsi:SignedProperties>";

    /// <summary>C14N (sin comentarios) de un XmlDocument → SHA-1 → Base64.</summary>
    private static string C14nSha1(XmlDocument doc)
    {
        var t = new XmlDsigC14NTransform();
        t.LoadInput(doc);
        using var s = (Stream)t.GetOutput(typeof(Stream));
        var bytes = ReadAll(s);
#pragma warning disable CA5350
        return Convert.ToBase64String(SHA1.HashData(bytes));
#pragma warning restore CA5350
    }

    /// <summary>Parsea el fragmento XML como documento y llama a C14nSha1.</summary>
    private static string C14nSha1(string xmlFragment)
    {
        var doc = new XmlDocument { PreserveWhitespace = false };
        doc.LoadXml(xmlFragment);
        return C14nSha1(doc);
    }

    /// <summary>
    /// C14N del SignedInfo → RSA-SHA1 → Base64.
    /// SignData hashea internamente con SHA-1 antes de aplicar RSA (PKCS#1 v1.5).
    /// </summary>
    private static string C14nRsaSha1(string signedInfoXml, RSA rsaKey)
    {
        var doc = new XmlDocument { PreserveWhitespace = false };
        doc.LoadXml(signedInfoXml);
        var t = new XmlDsigC14NTransform();
        t.LoadInput(doc);
        using var s = (Stream)t.GetOutput(typeof(Stream));
        var bytes    = ReadAll(s);
        var sigBytes = rsaKey.SignData(bytes, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
        return Convert.ToBase64String(sigBytes);
    }

    private static byte[] ReadAll(Stream stream)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    private static string EscapeXml(string value) =>
        value
            .Replace("&",  "&amp;",  StringComparison.Ordinal)
            .Replace("<",  "&lt;",   StringComparison.Ordinal)
            .Replace(">",  "&gt;",   StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("'",  "&apos;", StringComparison.Ordinal);
}

