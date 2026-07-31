// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Telemetry;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Framework.Tests.UnitTestHelpers.TestClasses;
using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline;

/// <summary>
/// Tests that an unexpected failure while generating the design-time source of one type does not suppress the
/// generated source of the whole project.
/// </summary>
/// <remarks>
/// See https://github.com/metalama/Metalama/issues/1767: a <see cref="NullReferenceException"/> raised while
/// rendering the attribute lists of an introduced constructor escaped the concurrent task runner and aborted the
/// entire design-time pipeline execution, so the user saw the generated source of every type in the project
/// disappear rather than losing a single member.
/// </remarks>
public sealed class DesignTimeSyntaxTreeGeneratorFailureTests : DesignTimePipelineTestsBase
{
    public DesignTimeSyntaxTreeGeneratorFailureTests( ITestOutputHelper logger ) : base( logger ) { }

    private const string _code = """
                                 using Metalama.Framework.Aspects;

                                 public class Aspect : TypeAspect
                                 {
                                     [Introduce]
                                     public void IntroducedMethod() { }
                                 }

                                 [Aspect]
                                 public partial class C { }
                                 """;

    [Fact]
    public void FailureOnOneGeneratedFileDoesNotSuppressTheOthers()
    {
        using var services = new AdditionalServiceCollection();
        services.AddProjectService<IConcurrentTaskRunner>( _ => new FailureInjectingTaskRunner(), allowOverride: true );

        using var testContext = this.CreateTestContext( services );
        testContext.ExpectsReportedExceptions = true;

        var compilation = testContext.CreateCSharpCompilation( _code );

        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        Assert.True( factory.TryExecute( testContext.ProjectOptions, compilation, default, out var results ) );

        // The generated source of the type that did not fail is still produced.
        var introductions = results.Result.SyntaxTreeResults.Values.SelectMany( r => r.Introductions ).ToList();
        var introduction = Assert.Single( introductions );
        Assert.Contains( "IntroducedMethod", introduction.GeneratedSyntaxTree.ToString(), StringComparison.Ordinal );

        // The failure is reported rather than silently ignored. It has no location, so it lands in the result of the
        // "empty" syntax tree rather than in the result of one of the input files.
        Assert.Contains(
            results.Result.SyntaxTreeResults.Values.SelectMany( r => r.Diagnostics ),
            d => d.Id == GeneralDiagnosticDescriptors.IgnorableUnhandledException.Id );

        // Containing the failure must not hide it from us: it also goes through the backstage exception handler, so a
        // crash report reaches telemetry instead of the defect being visible only in the user's error list.
        var report = Assert.Single( testContext.ReportedTelemetryExceptions );
        Assert.IsType<NullReferenceException>( report.Exception );
        Assert.Equal( TelemetryScenario.Exception, report.Scenario );
    }

    /// <summary>
    /// An <see cref="IConcurrentTaskRunner"/> that runs the items sequentially and, for the work items of
    /// <see cref="Metalama.Framework.Engine.Pipeline.DesignTime.DesignTimeSyntaxTreeGenerator"/>, first invokes the
    /// action on a default (therefore unresolvable) item, which makes the action throw.
    /// </summary>
    /// <remarks>
    /// This is the only seam that can deterministically inject a failure into the generation of a single file: the
    /// original defect is a data race and has no user-code shape that triggers it.
    /// </remarks>
    private sealed class FailureInjectingTaskRunner : IConcurrentTaskRunner
    {
        /// <summary>
        /// Prepends a poisoned item when the items are the per-file work items of the design-time syntax-tree
        /// generator. A default <see cref="KeyValuePair{TKey,TValue}"/> has a null key, so resolving it throws.
        /// </summary>
        private static IEnumerable<T> WithFailingItem<T>( IEnumerable<T> items )
        {
            if ( typeof(T) == typeof(KeyValuePair<IRef<INamespaceOrNamedType>, IEnumerable<ITransformation>>) )
            {
                yield return default!;
            }

            foreach ( var item in items )
            {
                yield return item;
            }
        }

        public Task RunConcurrentlyAsync<T>( IEnumerable<T> items, Action<T> action, CancellationToken cancellationToken )
            where T : notnull
        {
            foreach ( var item in WithFailingItem( items ) )
            {
                cancellationToken.ThrowIfCancellationRequested();
                action( item );
            }

            return Task.CompletedTask;
        }

        public Task RunConcurrentlyAsync<TItem, TContext>(
            IEnumerable<TItem> items,
            Action<TItem, TContext> action,
            Func<TContext> createContext,
            CancellationToken cancellationToken )
            where TItem : notnull
            where TContext : IDisposable
        {
            using var context = createContext();

            foreach ( var item in WithFailingItem( items ) )
            {
                cancellationToken.ThrowIfCancellationRequested();
                action( item, context );
            }

            return Task.CompletedTask;
        }

        public async Task RunConcurrentlyAsync<T>( IEnumerable<T> items, Func<T, Task> action, CancellationToken cancellationToken )
            where T : notnull
        {
            foreach ( var item in WithFailingItem( items ) )
            {
                cancellationToken.ThrowIfCancellationRequested();
                await action( item );
            }
        }
    }
}
