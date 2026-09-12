// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Xml;

#pragma warning disable CA5350, CA5350, CA5384, CA5351 // Do Not Use Weak Cryptographic Algorithms (TODO - but this means upgrading all license keys)

namespace Metalama.Backstage.Licensing.Licenses
{
    internal static class CryptographyHelper
    {
        /// <summary>
        /// Loads the XML representation of a key, with the processing of a document type definition and the resolution
        /// of external entities disabled.
        /// </summary>
        /// <param name="xmlString">The XML representation of a key.</param>
        /// <returns>The document of <paramref name="xmlString"/>.</returns>
        /// <remarks>
        /// The keys of the product are constants of this assembly, but <see cref="ExplicitLicensingAuthorityProvider"/>
        /// accepts a key from its caller, so the parser is hardened against an entity expansion and against the
        /// resolution of an external entity.
        /// </remarks>
        private static XmlDocument LoadKeyXml( string xmlString )
        {
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };

            using var stringReader = new StringReader( xmlString );
            using var xmlReader = XmlReader.Create( stringReader, settings );

            var xmlDocument = new XmlDocument { XmlResolver = null };
            xmlDocument.Load( xmlReader );

            return xmlDocument;
        }

        public static DSA CreateDsaFromXml( string xml ) => CreateDsaFromParameters( ParseDsaParameters( xml ) );

        private static DSA CreateDsaFromParameters( DSAParameters parameters )
        {
#if NET472 || NET5_0_OR_GREATER
            var dsa = DSA.Create( parameters );
#else
            var dsa = DSA.Create();
            dsa.ImportParameters( parameters );
#endif

            return dsa;
        }

        /// <summary>
        /// Reconstructs a System.Security.Cryptography.DSA object from an XML string.
        /// </summary>
        /// <remarks>
        /// This implementation supports .NET Core 2.1, where the <see cref="DSA" /> method is not implemented.
        /// </remarks>
        private static DSAParameters ParseDsaParameters( string xmlString )
        {
            static int ConvertByteArrayToInt( byte[] input )
            {
                // Input to this routine is always big endian
                var dwOutput = 0;

                // ReSharper disable once ForCanBeConvertedToForeach
                for ( var i = 0; i < input.Length; i++ )
                {
                    dwOutput *= 256;
                    dwOutput += input[i];
                }

                return dwOutput;
            }

            var parameters = default(DSAParameters);

            var xmlDoc = LoadKeyXml( xmlString );

            // ReSharper disable StringLiteralTypo

            // J is optional
            var missingNodes = new HashSet<string>
            {
                "P",
                "Q",
                "G",
                "Y",
                "Seed",
                "PgenCounter"
            };

            // ReSharper restore StringLiteralTypo

            if ( xmlDoc.DocumentElement!.Name.Equals( "DSAKeyValue", StringComparison.Ordinal ) )
            {
                foreach ( XmlNode? node in xmlDoc.DocumentElement.ChildNodes )
                {
                    if ( node == null )
                    {
                        throw new ArgumentException( $"Invalid key. Document contains null nodes.", nameof(xmlString) );
                    }

                    switch ( node.Name )
                    {
                        case "P":
                            parameters.P = Convert.FromBase64String( node.InnerText );
                            missingNodes.Remove( node.Name );

                            break;

                        case "Q":
                            parameters.Q = Convert.FromBase64String( node.InnerText );
                            missingNodes.Remove( node.Name );

                            break;

                        case "G":
                            parameters.G = Convert.FromBase64String( node.InnerText );
                            missingNodes.Remove( node.Name );

                            break;

                        case "Y":
                            parameters.Y = Convert.FromBase64String( node.InnerText );
                            missingNodes.Remove( node.Name );

                            break;

                        case "J":
                            parameters.J = Convert.FromBase64String( node.InnerText );
                            missingNodes.Remove( node.Name );

                            break;

                        case "X":
                            parameters.X = Convert.FromBase64String( node.InnerText );
                            missingNodes.Remove( node.Name );

                            break;

                        case "Seed":
                            parameters.Seed = Convert.FromBase64String( node.InnerText );
                            missingNodes.Remove( node.Name );

                            break;

                        // ReSharper disable once StringLiteralTypo
                        case "PgenCounter":
                            parameters.Counter = ConvertByteArrayToInt( Convert.FromBase64String( node.InnerText ) );
                            missingNodes.Remove( node.Name );

                            break;

                        default:
                            throw new ArgumentException( $"Invalid key. Unknown node: {node.Name}", nameof(xmlString) );
                    }
                }

                if ( missingNodes.Count != 0 )
                {
                    throw new ArgumentException( $"Invalid XML DSA key. Missing nodes: {string.Join( ", ", missingNodes )}", nameof(xmlString) );
                }
            }
            else
            {
                throw new ArgumentException( "Invalid XML DSA key.", nameof(xmlString) );
            }

            return parameters;
        }

        /// <summary>
        /// Gets the name of the root element of the XML representation of a key, which identifies the signature
        /// algorithm of that key.
        /// </summary>
        /// <param name="xmlString">The XML representation of a key.</param>
        /// <returns>The name of the root element of <paramref name="xmlString"/>.</returns>
        public static string GetKeyRootElementName( string xmlString ) => LoadKeyXml( xmlString ).DocumentElement!.Name;

        public static ECDsa CreateECDsaFromXml( string xml ) => CreateECDsaFromParameters( ParseECDsaParameters( xml ) );

        private static ECDsa CreateECDsaFromParameters( ECParameters parameters )
        {
#if NET472 || NET5_0_OR_GREATER
            var ecdsa = ECDsa.Create( parameters );
#else
            var ecdsa = ECDsa.Create();
            ecdsa.ImportParameters( parameters );
#endif

            return ecdsa;
        }

        /// <summary>
        /// Reconstructs the parameters of an Elliptic Curve DSA key from an XML string.
        /// </summary>
        /// <param name="xmlString">The XML representation of the key, whose root element is <c>ECDSAKeyValue</c>, and whose
        /// children are the friendly name of the curve, the two coordinates of the public point, and, for a private key,
        /// the private value.</param>
        /// <returns>The parameters of the key.</returns>
        /// <remarks>
        /// The key is parsed by hand, as the finite field DSA keys are, because the method that imports a public key in the
        /// SubjectPublicKeyInfo format, <c>ImportSubjectPublicKeyInfo</c>, is unavailable on the .NET Standard 2.0 and
        /// .NET Framework 4.7.2 targets of this assembly. The curve is resolved
        /// by mapping its name explicitly, and not through <see cref="ECCurve.CreateFromFriendlyName"/>, whose behavior
        /// differs between platforms.
        /// </remarks>
        private static ECParameters ParseECDsaParameters( string xmlString )
        {
            var parameters = default(ECParameters);

            var xmlDoc = LoadKeyXml( xmlString );

            if ( !xmlDoc.DocumentElement!.Name.Equals( "ECDSAKeyValue", StringComparison.Ordinal ) )
            {
                throw new ArgumentException( "Invalid XML Elliptic Curve DSA key.", nameof(xmlString) );
            }

            var missingNodes = new HashSet<string> { "Curve", "X", "Y" };
            var point = default(ECPoint);

            foreach ( XmlNode? node in xmlDoc.DocumentElement.ChildNodes )
            {
                if ( node == null )
                {
                    throw new ArgumentException( "Invalid key. Document contains null nodes.", nameof(xmlString) );
                }

                switch ( node.Name )
                {
                    case "Curve":
                        parameters.Curve = GetNamedCurve( node.InnerText, nameof(xmlString) );
                        missingNodes.Remove( node.Name );

                        break;

                    case "X":
                        point.X = Convert.FromBase64String( node.InnerText );
                        missingNodes.Remove( node.Name );

                        break;

                    case "Y":
                        point.Y = Convert.FromBase64String( node.InnerText );
                        missingNodes.Remove( node.Name );

                        break;

                    // D is present in a private key only.
                    case "D":
                        parameters.D = Convert.FromBase64String( node.InnerText );

                        break;

                    default:
                        throw new ArgumentException( $"Invalid key. Unknown node: {node.Name}", nameof(xmlString) );
                }
            }

            if ( missingNodes.Count != 0 )
            {
                throw new ArgumentException(
                    $"Invalid XML Elliptic Curve DSA key. Missing nodes: {string.Join( ", ", missingNodes )}",
                    nameof(xmlString) );
            }

            parameters.Q = point;

            return parameters;
        }

        /// <summary>
        /// Gets the curve of a given name.
        /// </summary>
        /// <param name="name">The friendly name of the curve.</param>
        /// <param name="parameterName">The name of the parameter that the name of the curve was read from.</param>
        /// <returns>The curve named <paramref name="name"/>.</returns>
        private static ECCurve GetNamedCurve( string name, string parameterName )
            => name switch
            {
                "nistP256" => ECCurve.NamedCurves.nistP256,
                _ => throw new ArgumentException( $"Invalid key. Unknown curve: {name}", parameterName )
            };
    }
}
