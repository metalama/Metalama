// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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