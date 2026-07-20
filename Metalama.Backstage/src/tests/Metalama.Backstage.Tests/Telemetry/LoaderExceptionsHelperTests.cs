// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Telemetry;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Xunit;

namespace Metalama.Backstage.Tests.Telemetry
{
    /// <summary>
    /// Unit tests for <see cref="LoaderExceptionsHelper"/> and for the <c>LoaderExceptions</c> element it feeds in
    /// <see cref="ExceptionXmlFormatter"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="Exception.ToString"/> does not render <see cref="ReflectionTypeLoadException.LoaderExceptions"/>, so
    /// without this capture a crash report never names the assembly that failed to bind.
    /// </remarks>
    public sealed class LoaderExceptionsHelperTests
    {
        /// <summary>
        /// Creates a <see cref="ReflectionTypeLoadException"/> carrying the given loader exceptions.
        /// </summary>
        private static ReflectionTypeLoadException CreateReflectionTypeLoadException( params Exception?[] loaderExceptions )
            => new( new Type?[loaderExceptions.Length], loaderExceptions );

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
        public void NoLoaderExceptionsWhenNotAReflectionTypeLoadException()
        {
            Assert.Empty( LoaderExceptionsHelper.GetLoaderExceptions( new InvalidOperationException( "test" ) ) );
            Assert.Null( LoaderExceptionsHelper.GetLoaderExceptionsText( new InvalidOperationException( "test" ) ) );
            Assert.Null( LoaderExceptionsHelper.GetLoaderExceptionsText( null ) );
        }

        [Fact]
        public void LoaderExceptionsAreDeduplicatedByTypeAndMessage()
        {
            // A ReflectionTypeLoadException normally carries one loader exception per failing type, so the same binding
            // failure is repeated once per type.
            var exception = CreateReflectionTypeLoadException(
                new FileNotFoundException( "Could not load 'A'." ),
                new FileNotFoundException( "Could not load 'A'." ),
                new FileNotFoundException( "Could not load 'B'." ) );

            var loaderExceptions = LoaderExceptionsHelper.GetLoaderExceptions( exception );

            Assert.Equal( 2, loaderExceptions.Length );
            Assert.Equal( ["Could not load 'A'.", "Could not load 'B'."], loaderExceptions.Select( e => e.Message ) );
        }

        [Fact]
        public void NullLoaderExceptionsAreSkipped()
        {
            var exception = CreateReflectionTypeLoadException( null, new FileNotFoundException( "Could not load 'A'." ), null );

            Assert.Single( LoaderExceptionsHelper.GetLoaderExceptions( exception ) );
        }

        [Fact]
        public void LoaderExceptionsAreFoundThroughInnerAndAggregateExceptions()
        {
            var inner = CreateReflectionTypeLoadException( new FileNotFoundException( "Could not load 'A'." ) );
            var aggregate = new AggregateException( new InvalidOperationException( "wrapper", inner ) );

            var loaderExceptions = LoaderExceptionsHelper.GetLoaderExceptions( aggregate );

            Assert.Single( loaderExceptions );
            Assert.Equal( "Could not load 'A'.", loaderExceptions[0].Message );
        }

        [Fact]
        public void ResultIsCappedAndTruncationIsReported()
        {
            var loaderExceptions = Enumerable.Range( 0, 100 )
                .Select( i => (Exception?) new FileNotFoundException( $"Could not load 'A{i}'." ) )
                .ToArray();

            var text = LoaderExceptionsHelper.GetLoaderExceptionsText( CreateReflectionTypeLoadException( loaderExceptions ) );

            Assert.NotNull( text );
            Assert.Contains( "truncated after", text, StringComparison.Ordinal );
        }

        [Fact]
        public void TextRenderingIncludesTypeAndMessageAndIsScrubbed()
        {
            var exception = CreateReflectionTypeLoadException( new FileNotFoundException( "Could not load 'Secret'." ) );

            var text = LoaderExceptionsHelper.GetLoaderExceptionsText( exception );

            Assert.NotNull( text );
            Assert.Contains( typeof(FileNotFoundException).FullName!, text, StringComparison.Ordinal );
            Assert.Contains( "Could not load 'Secret'.", text, StringComparison.Ordinal );

            // The scrubber is applied to the rendered messages.
            var scrubbed = LoaderExceptionsHelper.GetLoaderExceptionsText( exception, _ => "<redacted>" );

            Assert.NotNull( scrubbed );
            Assert.DoesNotContain( "Secret", scrubbed, StringComparison.Ordinal );
        }

        [Fact]
        public void LoaderExceptionsAreWrittenToXmlWithoutTheirMessage()
        {
            var exception = CreateReflectionTypeLoadException( new FileNotFoundException( "Could not load 'Secret'." ) );

            var xml = WriteExceptionXml( exception );

            // The XML must be well-formed.
            var doc = XDocument.Parse( xml );

            var loaderExceptions = doc.Descendants( "LoaderExceptions" ).SingleOrDefault();

            Assert.NotNull( loaderExceptions );

            // The exception type is what identifies the failure kind, and it is safe to upload.
            Assert.Equal(
                typeof(FileNotFoundException).FullName,
                loaderExceptions.Descendants( "Type" ).Single().Value );

            // The message can embed user paths, so the default scrubber withholds it from the upload payload, exactly as
            // it does for any other exception. See #1680.
            Assert.DoesNotContain( "Secret", xml, StringComparison.Ordinal );
            Assert.Empty( loaderExceptions.Descendants( "Message" ) );
        }

        [Fact]
        public void NoLoaderExceptionsElementWhenThereAreNone()
        {
            var xml = WriteExceptionXml( new InvalidOperationException( "test" ) );

            Assert.Empty( XDocument.Parse( xml ).Descendants( "LoaderExceptions" ) );
        }
    }
}
