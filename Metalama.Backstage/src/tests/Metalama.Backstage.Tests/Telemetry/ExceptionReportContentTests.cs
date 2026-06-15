// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Telemetry;
using System;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Xunit;

namespace Metalama.Backstage.Tests.Telemetry
{
    // Unit tests for the conservative redaction applied to the remote exception report payload (#1680).
    public sealed class ExceptionReportContentTests
    {
        private static string WriteExceptionXml( Exception exception )
        {
            var builder = new StringBuilder();

            using ( var writer = XmlWriter.Create( builder, new XmlWriterSettings { Indent = true } ) )
            {
                writer.WriteStartElement( "Exception" );
                ExceptionXmlFormatter.WriteException( writer, exception );
                writer.WriteEndElement();
            }

            return builder.ToString();
        }

        [Fact]
        public void ExceptionDataValuesAreRedactedButKeysAreKept()
        {
            var exception = new InvalidOperationException( "test" );
            exception.Data["ConnectionString"] = "Server=db;Password=p@ssw0rd;User=sa";
            exception.Data["CustomerName"] = "Jane Doe";

            var xml = WriteExceptionXml( exception );

            // The XML must be well-formed.
            _ = XDocument.Parse( xml );

            // Values of non-allow-listed keys must be redacted, whatever their shape.
            Assert.DoesNotContain( "p@ssw0rd", xml, StringComparison.Ordinal );
            Assert.DoesNotContain( "Jane Doe", xml, StringComparison.Ordinal );
            Assert.Contains( "#redacted", xml, StringComparison.Ordinal );

            // The (scrubbed) keys are preserved so reviewers can see which entries existed.
            Assert.Contains( "ConnectionString", xml, StringComparison.Ordinal );
            Assert.Contains( "CustomerName", xml, StringComparison.Ordinal );
        }

        [Fact]
        public void ExceptionMessageSecretsAreRedacted()
        {
            var exception = new InvalidOperationException( "Login failed: password=SuperSecret123" );

            var xml = WriteExceptionXml( exception );

            Assert.DoesNotContain( "SuperSecret123", xml, StringComparison.Ordinal );
            Assert.Contains( "#secret", xml, StringComparison.Ordinal );
        }

        [Theory]
        [InlineData( "System.Private.CoreLib", true )]
        [InlineData( "System", true )]
        [InlineData( "Microsoft.CodeAnalysis", true )]
        [InlineData( "Metalama.Framework", true )]
        [InlineData( "mscorlib", true )]
        [InlineData( "netstandard", true )]
        [InlineData( "PostSharp.Patterns.Common", true )]
        [InlineData( "MyCompany.MyProduct", false )]
        [InlineData( "MyApp", false )]
        [InlineData( "SystemwideTool", false )]
        [InlineData( "", false )]
        [InlineData( null, false )]
        public void AssemblyNameClassification( string? name, bool expectedSafe )
            => Assert.Equal( expectedSafe, ExceptionReporter.IsKnownSafeAssemblyName( name ) );

        [Fact]
        public void UserAssemblyDetailsAreRedacted()
        {
            var builder = new StringBuilder();

            using ( var writer = XmlWriter.Create( builder, new XmlWriterSettings { Indent = true } ) )
            {
                writer.WriteStartElement( "Assemblies" );

                // A single-token user assembly name with a version and a file version.
                ExceptionReporter.WriteAssemblyElement( writer, "MyApp", new Version( 1, 2, 3, 4 ), "1.2.3.4-customer" );

                writer.WriteEndElement();
            }

            var xml = builder.ToString();
            var doc = XDocument.Parse( xml );

            Assert.DoesNotContain( "MyApp", xml, StringComparison.Ordinal );
            Assert.DoesNotContain( "1.2.3.4", xml, StringComparison.Ordinal );
            Assert.DoesNotContain( "customer", xml, StringComparison.Ordinal );
            Assert.Equal( "#user", doc.Descendants( "Name" ).Single().Value );
            Assert.Empty( doc.Descendants( "Version" ) );
            Assert.Empty( doc.Descendants( "FileVersion" ) );
        }

        [Fact]
        public void FrameworkAssemblyDetailsArePreserved()
        {
            var builder = new StringBuilder();

            using ( var writer = XmlWriter.Create( builder, new XmlWriterSettings { Indent = true } ) )
            {
                writer.WriteStartElement( "Assemblies" );
                ExceptionReporter.WriteAssemblyElement( writer, "System.Private.CoreLib", new Version( 8, 0, 0, 0 ), "8.0.0.0" );
                writer.WriteEndElement();
            }

            var xml = builder.ToString();
            var doc = XDocument.Parse( xml );

            Assert.Equal( "System.Private.CoreLib", doc.Descendants( "Name" ).Single().Value );
            Assert.Equal( "8.0.0.0", doc.Descendants( "Version" ).Single().Value );
        }
    }
}
