// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Compiler;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.Pipeline.CompileTime;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Aspects;

/// <summary>
/// Tests of <see cref="AspectBuilderState"/>.
/// </summary>
public sealed class AspectBuilderStateTests : UnitTestClass
{
    /// <summary>
    /// Verifies that a query that an aspect stores and uses after its <c>BuildAspect</c> method has completed throws, instead of adding a
    /// contributor that would not be part of the result of the aspect instance (change F12).
    /// </summary>
    [Fact]
    public async Task AddContributor_AfterToResult_Throws()
    {
        const string code = """
                            using Metalama.Framework.Aspects;
                            using Metalama.Framework.Code;
                            using Metalama.Framework.Diagnostics;
                            using Metalama.Framework.Fabrics;

                            [assembly: AspectOrder( AspectOrderDirection.CompileTime, typeof(StoringAspect), typeof(UsingAspect) )]

                            [CompileTime]
                            internal static class Storage
                            {
                                public static readonly DiagnosticDefinition Warning = new( "MY001", Severity.Warning, "Warning." );

                                public static IQuery<INamedType>? Receiver;
                            }

                            internal class StoringAspect : TypeAspect
                            {
                                public override void BuildAspect( IAspectBuilder<INamedType> builder ) => Storage.Receiver = builder.Outbound;
                            }

                            internal class UsingAspect : TypeAspect
                            {
                                public override void BuildAspect( IAspectBuilder<INamedType> builder )
                                    => Storage.Receiver!.ReportDiagnostic( _ => Storage.Warning );
                            }

                            [StoringAspect]
                            [UsingAspect]
                            internal class C { }
                            """;

        using var testContext = this.CreateTestContext();

        var pipeline = new CompileTimeAspectPipeline( testContext.ServiceProvider );
        var compilation = testContext.CreateCSharpCompilation( code );
        var diagnostics = new List<Diagnostic>();

        var result = await pipeline.ExecuteAsync( diagnostics.Add, null, compilation, ImmutableArray<ManagedResource>.Empty );

        // The exception is reported as an error of the aspect that used the query, and the pipeline itself completes.
        Assert.True( result.IsSuccessful );

        Assert.Contains(
            diagnostics,
            d => d.Severity == DiagnosticSeverity.Error
                 && d.GetMessage( CultureInfo.InvariantCulture ).Contains( "belongs to a different execution context", StringComparison.Ordinal ) );
    }
}
