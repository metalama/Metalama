// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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