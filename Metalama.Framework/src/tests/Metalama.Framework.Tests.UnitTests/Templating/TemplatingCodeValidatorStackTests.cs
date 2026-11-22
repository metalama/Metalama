// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Observers;
using Metalama.Framework.Engine.Templating;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Templating;

/// <summary>
/// Tests that <see cref="TemplatingCodeValidator"/> does not exhaust the stack when Roslyn binds a method body that contains
/// a long chain of calls (issue #2412).
/// </summary>
/// <remarks>
/// <para>
/// The first semantic model query in a method body makes Roslyn bind the whole body and run the nullability analysis. These
/// operations are recursive, and the amount of stack that they use grows with the depth of the deepest expression of the
/// body. The recursion guard of <see cref="SafeSyntaxWalker"/> does not account for this amount, so the validator must make
/// the first query at a shallow depth of the syntax walk.
/// </para>
/// <para>
/// The test first runs the validator with a large stack, and measures the amount of stack that the syntax walk uses to
/// reach the deepest node. This amount depends on the runtime and on the build configuration. The test then runs the
/// validator again with this amount plus <see cref="_stackAvailableAtDeepestNode"/> bytes. This is more than the recursion
/// guard requires, so the guard does not move the walk to another thread, but it is less than Roslyn requires to bind the
/// body. The validator therefore fails if it makes the first query at the deepest node.
/// </para>
/// <para>
/// Roslyn does not protect all of its recursive algorithms against the exhaustion of the stack. When this test fails, it
/// can therefore terminate the test process with a stack overflow instead of reporting an
/// <see cref="InsufficientExecutionStackException"/>.
/// </para>
/// </remarks>
public sealed class TemplatingCodeValidatorStackTests : UnitTestClass
{
    /// <summary>
    /// The number of calls in the chain of calls of the method body. Binding a body that contains a chain of 1,000 calls
    /// requires about 550 KB of stack on .NET 10 in the Debug configuration.
    /// </summary>
    private const int _chainLength = 1000;

    /// <summary>
    /// The stack size, in bytes, that the second run of the validator has when it reaches the deepest node of the method body.
    /// It is larger than the stack size below which the recursion guard of <see cref="SafeSyntaxWalker"/> moves the walk to
    /// another thread (128 KB), and smaller than the stack size that Roslyn requires to bind the body.
    /// </summary>
    private const long _stackAvailableAtDeepestNode = 256 * 1024;

    /// <summary>
    /// The stack size of the threads that run the validator.
    /// </summary>
    private const int _threadStackSize = 8 * 1024 * 1024;

    [Fact]
    public void LongCallChainWithLimitedStack()
    {
        var observer = new StackObserver();
        var services = CreateAdditionalServiceCollection();
        services.AddGlobalService<ITemplatingCodeValidatorObserver>( observer );

        using var testContext = this.CreateTestContext( services );

        // The compilation is created on a thread with a large stack because Roslyn reports its diagnostics, which requires
        // binding the method body.
        var compilation = RunOnThread( () => testContext.CreateCSharpCompilation( CreateCode() ) );

        // Measure the stack that the walk uses to reach the deepest node, with a stack that is large enough for any order of
        // the semantic model queries.
        var (startStackSize, measuredDiagnostics) = RunOnThread(
            () =>
            {
                Assert.True( StackLimits.TryGetAvailableStackSize( out var available ) );
                observer.Reset();

                return (available, Validate( testContext, compilation ));
            } );

        Assert.Empty( measuredDiagnostics );

        var walkStackSize = startStackSize - observer.MinAvailableStackSize;

        // Run the validator again with just enough stack to reach the deepest node.
        var diagnostics = RunOnThread(
            () => ConsumeStackUntil( walkStackSize + _stackAvailableAtDeepestNode, () => Validate( testContext, compilation ) ) );

        Assert.Empty( diagnostics );
    }

    /// <summary>
    /// Runs the validator on the only syntax tree of the given compilation, with a new semantic model, and returns the
    /// errors that it reports.
    /// </summary>
    private static List<Diagnostic> Validate( MetalamaTestContext testContext, CSharpCompilation compilation )
    {
        var diagnostics = new List<Diagnostic>();

        TemplatingCodeValidator.Validate(
            testContext.ServiceProvider,
            compilation.GetSemanticModel( compilation.SyntaxTrees[0] ),
            diagnostics.Add,
            null,
            false,
            false,
            testContext.CancellationToken );

        return diagnostics.Where( d => d.Severity == DiagnosticSeverity.Error ).ToList();
    }

    /// <summary>
    /// Creates the code of a class whose method body contains a chain of <see cref="_chainLength"/> calls.
    /// </summary>
    private static string CreateCode()
    {
        var code = new StringBuilder();
        code.AppendLine( "#nullable enable" );
        code.Append( "class C { C M() => this; void F() { this" );

        for ( var i = 0; i < _chainLength; i++ )
        {
            code.Append( ".M()" );
        }

        code.Append( "; } }" );

        return code.ToString();
    }

    /// <summary>
    /// Runs the given function on a new thread with a stack of <see cref="_threadStackSize"/> bytes, and returns its result
    /// or rethrows its exception.
    /// </summary>
    private static T RunOnThread<T>( Func<T> func )
    {
        var result = default(T);
        ExceptionDispatchInfo? exception = null;

        var thread = new Thread(
            () =>
            {
                try
                {
                    result = func();
                }
                catch ( Exception e )
                {
                    exception = ExceptionDispatchInfo.Capture( e );
                }
            },
            _threadStackSize );

        thread.Start();
        thread.Join();

        exception?.Throw();

        return result!;
    }

    /// <summary>
    /// Calls itself recursively until the available stack size is at most <paramref name="availableStackSize"/> bytes,
    /// then calls the given function and returns its result.
    /// </summary>
    [MethodImpl( MethodImplOptions.NoInlining )]
    private static T ConsumeStackUntil<T>( long availableStackSize, Func<T> func )
    {
        Assert.True( StackLimits.TryGetAvailableStackSize( out var current ) );

        if ( current <= availableStackSize )
        {
            return func();
        }

        Span<byte> buffer = stackalloc byte[1024];
        buffer[0] = 1;

        var result = ConsumeStackUntil( availableStackSize, func );

        // Reading the buffer after the call prevents the call from being compiled as a tail call.
        Volatile.Read( ref buffer[0] );

        return result;
    }

    /// <summary>
    /// An <see cref="ITemplatingCodeValidatorObserver"/> that records the smallest available stack size at which the
    /// validator queries the semantic model.
    /// </summary>
    private sealed class StackObserver : ITemplatingCodeValidatorObserver
    {
        /// <summary>
        /// Gets the smallest available stack size, in bytes, at which the validator has queried the semantic model since the
        /// last call of <see cref="Reset"/>.
        /// </summary>
        public long MinAvailableStackSize { get; private set; } = long.MaxValue;

        /// <summary>
        /// Clears the recorded value.
        /// </summary>
        public void Reset() => this.MinAvailableStackSize = long.MaxValue;

        /// <inheritdoc />
        public void OnSemanticModelUsed()
        {
            if ( StackLimits.TryGetAvailableStackSize( out var available ) && available < this.MinAvailableStackSize )
            {
                this.MinAvailableStackSize = available;
            }
        }

        /// <inheritdoc />
        public void OnSymbolClassifierUsed( bool isQuickMode ) { }

        /// <inheritdoc />
        public void OnSyntaxTreeValidated() { }

        /// <inheritdoc />
        public void OnSyntaxTreeSkipped() { }
    }
}
