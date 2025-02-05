// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Diagnostics;
using Microsoft.CodeAnalysis;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Diagnostics
{
    public sealed class DiagnosticListTests
    {
        [Fact]
        public void Add()
        {
            DiagnosticBag bag = new();

            var diagnostic = Diagnostic.Create(
                "id",
                "category",
                new NonLocalizedString( "message" ),
                DiagnosticSeverity.Error,
                DiagnosticSeverity.Error,
                true,
                0 );

            bag.Report( diagnostic );
            Assert.Single( bag, diagnostic );
            Assert.Same( diagnostic, bag.Single() );
        }
    }
}