// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Diagnostics;
using System.Globalization;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Diagnostics
{
    public sealed class DiagnosticTests
    {
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
    }
}