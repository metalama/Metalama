// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Xml;

#pragma warning disable CA5350, CA5350, CA5384, CA5351 // Do Not Use Weak Cryptographic Algorithms (TODO - but this means upgrading all license keys)

namespace Metalama.Backstage.Licensing.Licenses
{
    internal static class CryptographyHelper
    {
        public static DSA CreateDsaFromXml( string xml ) => CreateDsaFromParameters( ParseDsaParameters( xml ) );

        public static DSA CreateDsaFromParameters( DSAParameters parameters )
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

            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml( xmlString );

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
    }
}