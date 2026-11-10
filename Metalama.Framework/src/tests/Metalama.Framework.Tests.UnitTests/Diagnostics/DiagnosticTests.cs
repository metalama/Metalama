// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Diagnostics;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.Diagnostics;
using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Diagnostics
{
    public sealed class DiagnosticTests
    {
        // Diagnostics that have no documentation page yet, and therefore no help link.
        private static readonly HashSet<string> _undocumentedDiagnostics = ["LAMA0121", "LAMA0889", "LAMA0890", "LAMA0891", "LAMA0892"];

        [Fact]
        public void StandardDiagnosticDescriptors()
        {
            var args = new object[32];

            // This should at least test that there is no duplicate.
            _ = DesignTimeDiagnosticDefinitions.StandardDiagnosticDescriptors;

            // Test that the formatting strings are valid.
            foreach ( var descriptor in DesignTimeDiagnosticDefinitions.StandardDiagnosticDescriptors.Values )
            {
                var formattingString = descriptor.MessageFormat.ToString( CultureInfo.InvariantCulture );
                _ = string.Format( CultureInfo.InvariantCulture, formattingString, args );
            }
        }

        [Fact]
        public void StandardDiagnosticDescriptorsHaveHelpLink()
        {
            foreach ( var descriptor in DesignTimeDiagnosticDefinitions.StandardDiagnosticDescriptors.Values )
            {
                if ( !descriptor.Id.StartsWith( "LAMA", System.StringComparison.Ordinal ) )
                {
                    continue;
                }

                // Roslyn stores a null help link as an empty string.
                var expected = _undocumentedDiagnostics.Contains( descriptor.Id )
                    ? ""
                    :$"https://doc.postsharp.net/metalama/errors/{descriptor.Id.ToLowerInvariant()}";

                Assert.True( expected == descriptor.HelpLinkUri, $"{descriptor.Id}: expected '{expected}', got '{descriptor.HelpLinkUri}'." );
            }
        }

        [Fact]
        public void HelpLinkPlaceholderIsReplacedByLowerCaseId()
        {
            var definition = new DiagnosticDefinition<string>( "MY001", Severity.Warning, "Message {0}.", null, null, "https://example.com/errors/{0}" );

            Assert.Equal( "https://example.com/errors/my001", definition.HelpLinkUri );
            Assert.Equal( "https://example.com/errors/my001", definition.ToRoslynDescriptor().HelpLinkUri );
            Assert.Equal( "https://example.com/errors/my001", definition.CreateRoslynDiagnostic( null, "x" ).Descriptor.HelpLinkUri );
        }

        [Fact]
        public void HelpLinkWithoutPlaceholderIsKept()
        {
            var definition = new DiagnosticDefinition( "MY002", Severity.Warning, "Message.", null, null, "https://example.com/my-page" );

            Assert.Equal( "https://example.com/my-page", definition.HelpLinkUri );
        }

        [Fact]
        public void HelpLinkIsNullByDefault()
        {
            var definition = new DiagnosticDefinition( "MY003", Severity.Warning, "Message." );

            Assert.Null( definition.HelpLinkUri );
            Assert.Equal( "", definition.ToRoslynDescriptor().HelpLinkUri );
        }
    }
}
