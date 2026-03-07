using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace BeyondTrust.SecretSafeProvider;

public static class CertificateGenerator
{
    private const int KeyStrength = 2048;

    public static X509Certificate2 GenerateSelfSignedCertificate(string subjectName, string issuerName)
    {
        using var rsa = RSA.Create(KeyStrength);

        var subject = new X500DistinguishedName(subjectName);
        var request = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(false, false, 0, false));

        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, false));

        request.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension(
                [new Oid("1.3.6.1.5.5.7.3.1")], false)); // TLS Server Authentication

        var san = new SubjectAlternativeNameBuilder();
        san.AddDnsName("localhost");
        san.AddIpAddress(IPAddress.Loopback);
        request.CertificateExtensions.Add(san.Build());

        var notBefore = DateTimeOffset.UtcNow.AddMinutes(-5);
        var notAfter = DateTimeOffset.UtcNow.AddYears(2);

        var cert = request.CreateSelfSigned(notBefore, notAfter);

        return X509CertificateLoader.LoadPkcs12(
            cert.Export(X509ContentType.Pkcs12), null,
            X509KeyStorageFlags.Exportable);
    }
}
