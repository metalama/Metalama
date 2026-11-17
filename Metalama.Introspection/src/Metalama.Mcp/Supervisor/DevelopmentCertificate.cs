// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Metalama.Mcp.Supervisor;

/// <summary>
/// Finds the ASP.NET Core development certificate, with which the result server serves <c>https://localhost</c>. The
/// certificate is created and trusted by <c>dotnet dev-certs https --trust</c>, or by Visual Studio.
/// </summary>
internal static class DevelopmentCertificate
{
    /// <summary>
    /// The extension that marks the ASP.NET Core development certificate.
    /// </summary>
    private const string DevelopmentCertificateOid = "1.3.6.1.4.1.311.84.1.1";

    /// <summary>
    /// A test-only override: the path of a PFX file, or <c>missing</c> to act as if there were no development certificate.
    /// </summary>
    private const string TestCertificateVariable = "METALAMA_MCP_TEST_CERTIFICATE";

    public const string MissingMessage =
        "The html format serves the result on https://localhost and needs the trusted ASP.NET Core development certificate, which was not found. "
        + "Ask the user to run `dotnet dev-certs https --trust`, then retry. Use the text or json format meanwhile.";

    public static bool TryGet( [NotNullWhen( true )] out X509Certificate2? certificate, [NotNullWhen( false )] out string? error )
    {
        var testCertificate = Environment.GetEnvironmentVariable( TestCertificateVariable );

        if ( !string.IsNullOrEmpty( testCertificate ) )
        {
            certificate = testCertificate == "missing" ? null : X509CertificateLoader.LoadPkcs12FromFile( testCertificate, null );
            error = certificate == null ? MissingMessage : null;

            return certificate != null;
        }

        certificate = Find();
        error = certificate == null ? MissingMessage : null;

        return certificate != null;
    }

    private static X509Certificate2? Find()
    {
        try
        {
            using var store = new X509Store( StoreName.My, StoreLocation.CurrentUser );
            store.Open( OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly );

            var now = DateTime.Now;

            var candidate = store.Certificates
                .Where( c => c.HasPrivateKey && c.NotBefore <= now && now < c.NotAfter && c.Extensions.Any( e => e.Oid?.Value == DevelopmentCertificateOid ) )
                .MaxBy( c => c.NotAfter );

            // On Windows, a certificate that is not in the trusted root store makes the browser show a warning. On other
            // platforms, trust is recorded per browser or in the keychain, where it cannot be checked reliably.
            if ( candidate != null && OperatingSystem.IsWindows() && !IsTrustedOnWindows( candidate ) )
            {
                return null;
            }

            return candidate;
        }
        catch ( CryptographicException )
        {
            return null;
        }
    }

    private static bool IsTrustedOnWindows( X509Certificate2 certificate )
    {
        using var root = new X509Store( StoreName.Root, StoreLocation.CurrentUser );
        root.Open( OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly );

        return root.Certificates.Any( c => c.Thumbprint == certificate.Thumbprint );
    }
}
