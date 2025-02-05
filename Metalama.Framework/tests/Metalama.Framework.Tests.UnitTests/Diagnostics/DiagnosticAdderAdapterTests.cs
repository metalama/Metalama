// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Diagnostics;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Diagnostics
{
    public sealed class DiagnosticAdderAdapterTests
    {
        [Fact]
        public void Add()
        {
            List<Diagnostic> list = new();
            DiagnosticAdderAdapter adapter = new( list.Add );

            var diagnostic = Diagnostic.Create(
                "id",
                "category",
                new NonLocalizedString( "message" ),
                DiagnosticSeverity.Error,
                DiagnosticSeverity.Error,
                true,
                0 );

            adapter.Report( diagnostic );

            Assert.Single( list, diagnostic );
        }
    }
}