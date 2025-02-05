// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Utilities;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

#pragma warning disable CA5350, CA5350, CA5384, CA5351 // Do Not Use Weak Cryptographic Algorithms (TODO - but this means upgrading all license keys)

namespace Metalama.Backstage.Licensing.Licenses
{
    /// <summary>
    /// Utility cryptographic methods for use with the PostSharp licensing system.
    /// </summary>
    internal static class LicenseCryptography
    {
        private static readonly DSA _productionPublicKey0 = DSA.Create();
        private static readonly DSA _productionPublicKey1 = DSA.Create();

        /// <summary>
        /// Initializes static members of the <see cref="LicenseCryptography"/> class.
        /// </summary>
        static LicenseCryptography()
        {
            // ReSharper disable StringLiteralTypo

            // FromXmlString method is not supported by .NET Core
            _productionPublicKey0.FromXmlString2(
                "<DSAKeyValue><P>9cMyBYBokidciAghqE1POnEbcxpBui3PfazddrQjndkDtPskGvBcjS8LIStB/jR0SICKmLMwl7WoocpdXgYTOopgKJ33E4NOIhc1vbQR6vCCidGWlN88hUKCQJ8cGzme/LDmUT5zfK3TfM6LkMU1fYTNARrefIZkSlg4GGIjZ38=</P><Q>m9h5p2kl1vlwuw12AOQbem3yDXU=</Q><G>pBkhekdI1vk084zMbubnu7qtDyTid6x01crQJiERfmk2HgFt13dXHwei/1kgrRJPWrtZVRKMmO8w+p4jfle82n2/BaFNBLouUoQ/fBYPPdDZBocd/tXqBduF5zq1S12tDv8TIIarMTRtj18F5e68cxBPbweVs4n8meqLEQL5AwA=</G><Y>e2otaOKaVFxnEoHI4g1f7BCcrOaAwd1/GTMkEXGaNw3CYucIuOJdvlZEWa/pa4DTUeK4McHOXRJsZMQdHaoh+dK17NdmMxTa2UMokyoIdayu9kw9TbWUy2zXovJ8CHJVP4RU8wlJk1RKjeMuSK3lYPgo2RTbV9UbU2qK1gmVwg4=</Y><J>AAAAAZOzu4FkAIr0MjlqqHtPNWrFTfjw4/qDWuFvHEf7ioaj8vqRao8mbqsLueqvYIYQ8g8w2WNWFAOG6e8waiQhX2O/DRSZNbc/JfdjQqlPli5be6FqNsGnjKXdEt2boONKU/fpGx/m69V+a/4jxg==</J><Seed>1B0yRR/A/kmE1zMUIFiEMmJ328M=</Seed><PgenCounter>Xg==</PgenCounter></DSAKeyValue>" );

            _productionPublicKey1.FromXmlString2(
                "<DSAKeyValue><P>vAmBC+eZJaZa7HdlTDAgsfcT0QSjqN8d8fEeZ9E1kxfIAYGerlHFHW/A5muBYy8FyO7W8r4mqxpxcvFQEeEqVe89BUXecHjh6FkTEsT25r/nbV4jnZBxNz16qb7A6t8MCr0jzuzrIGFVP5VG/ad0s/1078WqpwQqJQXHmH/lXX0=</P><Q>+RdtGnwCJw4u2H/goSLtaAGr1U0=</Q><G>sxQQgHIuRgYOMtB+r7EGRO/OTRGXhUrFyZ1R9nVerGGC2juEVWSoydr2JquILOwIO7+1kIOwbkhCjNlZIAdvWRlN5COF7gHfPi1dSX7LzDcNbZDADvrOUmk1KG3hZ3Vf67XIbug2/nq8aij7gbEs4eA26EWWpObO0a+e2QmsQII=</G><Y>dP073SH4QG5KiV5BbZEDLiV3/D2eD18D9jsMVD1p+eMZsffU88/Pxfen1Pe5cyulw8gQkEvlAa3GEmGsaGaa7Qp245NPD8fbEOLFu3tdwMhw/ylRHpjTS7BDRjvGeyGwSS0WTWQCwCyI8LN6Rvg7p4RfhHIaAWWkTJNVAG7AN7g=</Y><J>wUCV+9KzxPW+J3/DIm3sIfVf29Z8u5zPXnEZbMTrkWwdgOTSPuXimtiQku8knyWD3iC+GqyhtoFqdgXqQS6WcadAABb2U5mMTL0V1o6Jy6c0cyPb9blmf5wdZxMKVlXe9lcAO8rP16XhQGVs</J><Seed>h7zytTPqA9Ue3F7c/j+9iXW4Ebw=</Seed><PgenCounter>Aag=</PgenCounter></DSAKeyValue>" );

            // ReSharper restore StringLiteralTypo
        }

        /// <summary>
        /// Reconstructs a System.Security.Cryptography.DSA object from an XML string.
        /// </summary>
        /// <remarks>
        /// This implementation supports .NET Core 2.1, where the <see cref="DSA" /> method is not implemented.
        /// </remarks>
        private static void FromXmlString2( this DSA dsa, string xmlString, bool expectPrivateParameters = false )
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

            if ( expectPrivateParameters )
            {
                missingNodes.Add( "X" );
            }

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
                            if ( !expectPrivateParameters )
                            {
                                // We check this so a private key is not accidentally disclosed.
                                throw new ArgumentException( $"Invalid public key.", nameof(xmlString) );
                            }

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

            dsa.ImportParameters( parameters );
        }

        /// <summary>
        /// Computes an invariant 64-bit hash of a string.
        /// </summary>
        /// <param name="s">A string.</param>
        /// <returns>An invariant 64-bit hash of <paramref name="s"/>.</returns>
        public static long ComputeStringHash64( string? s )
        {
            if ( s == null )
            {
                return 0;
            }

            s = s.Trim().ToLowerInvariant().Normalize();
            var bytes = Encoding.UTF8.GetBytes( s );

            byte[] hash;

            using ( var md5 = new MD5Managed() )
            {
                hash = md5.ComputeHash( bytes );
            }

            long hash64;

            unsafe
            {
                fixed ( byte* p = hash )
                {
                    hash64 = *(long*) p;
                }
            }

            // Make sure we never return 0 for a non-null string.
            if ( hash64 == 0 )
            {
                hash64 = -1;
            }

            return hash64;
        }

        private static byte[] GetHash( byte[] message )
        {
            using ( var sha1 = SHA1.Create() )
            {
                return sha1.ComputeHash( message );
            }
        }

        /// <summary>
        /// Verifies the signature of a message given a <see cref="DSA"/>.
        /// </summary>
        /// <param name="message">Message.</param>
        /// <param name="publicKey">Public key.</param>
        /// <param name="signature">Signature of <paramref name="message"/> generated with the private key corresponding to <paramref name="publicKey"/>.</param>
        /// <returns><c>true</c> if the signature is valid, otherwise <c>false</c>.</returns>
        internal static bool VerifySignature( byte[] message, DSA publicKey, byte[] signature )
        {
            return publicKey.VerifySignature( GetHash( message ), signature );
        }

        /// <summary>
        /// Gets public key for the given <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The index of the requested public key.</param>
        /// <returns>The public key for the given <paramref name="index"/>.</returns>
        internal static DSA GetPublicKey( byte index )
        {
            switch ( index )
            {
                case 0:
                    return _productionPublicKey0;

                case 1:
                    return _productionPublicKey1;

                // LicensingMessageSource.Instance.Write( MessageLocation.Unknown, SeverityType.Warning, "PS0128", new object[0] );

                default:
                    throw new ArgumentOutOfRangeException( nameof(index) );
            }
        }

        /// <summary>
        /// Signs a message.
        /// </summary>
        /// <param name="message">Message.</param>
        /// <param name="privateKeyXml">Private key, encoded as XML.</param>
        /// <returns>The signature of <paramref name="message"/>.</returns>
        internal static byte[] Sign( byte[] message, string privateKeyXml )
        {
            // Create the signature.
            using ( var privateKey = DSA.Create() )
            {
                privateKey.FromXmlString2( privateKeyXml, true );

                return privateKey.CreateSignature( GetHash( message ) );
            }
        }
    }
}