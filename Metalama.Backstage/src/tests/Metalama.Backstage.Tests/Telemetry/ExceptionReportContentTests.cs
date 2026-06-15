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
        public void ExceptionDataIsNotSentAtAll()
        {
            var exception = new InvalidOperationException( "test" );
            exception.Data["ConnectionString"] = "Server=db;Password=p@ssw0rd;User=sa";
            exception.Data["CustomerName"] = "Jane Doe";

            var xml = WriteExceptionXml( exception );

            // The XML must be well-formed.
            var doc = XDocument.Parse( xml );

            // Exception.Data is never serialized: neither values nor keys leave the machine, and no
            // Data element is emitted at all. See #1680.
            Assert.DoesNotContain( "p@ssw0rd", xml, StringComparison.Ordinal );
            Assert.DoesNotContain( "Jane Doe", xml, StringComparison.Ordinal );
            Assert.DoesNotContain( "ConnectionString", xml, StringComparison.Ordinal );
            Assert.DoesNotContain( "CustomerName", xml, StringComparison.Ordinal );
            Assert.Empty( doc.Descendants( "Data" ) );
        }

        [Fact]
        public void ExceptionMessageIsNotSentAtAll()
        {
            var exception = new InvalidOperationException( "Login failed: password=SuperSecret123 for user jane.doe" );

            var xml = WriteExceptionXml( exception );

            // The XML must be well-formed.
            var doc = XDocument.Parse( xml );

            // Exception.Message is never serialized: it frequently embeds user input, paths or secrets, so neither
            // the message text nor a Message element leaves the machine. See #1680.
            Assert.DoesNotContain( "SuperSecret123", xml, StringComparison.Ordinal );
            Assert.DoesNotContain( "Login failed", xml, StringComparison.Ordinal );
            Assert.Empty( doc.Descendants( "Message" ) );
        }

        [Theory]
        [InlineData( "System.Private.CoreLib", true )]
        [InlineData( "System", true )]
        [InlineData( "Microsoft.CodeAnalysis", true )]
        [InlineData( "Metalama.Framework", true )]
        [InlineData( "mscorlib", true )]
        [InlineData( "netstandard", true )]
        [InlineData( "PostSharp.Patterns.Common", true )]
        [InlineData( "Spectre.Console", true )]
        [InlineData( "Newtonsoft.Json", true )]
        [InlineData( "EnvDTE80", true )]
        [InlineData( "PresentationFramework", true )]
        [InlineData( "WindowsFormsIntegration", true )]
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
