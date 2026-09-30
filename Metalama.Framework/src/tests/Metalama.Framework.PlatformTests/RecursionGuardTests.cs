// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Framework.Tests.PlatformTests
{
    /// <summary>
    /// Tests for the recursion guard of <see cref="SafeSyntaxWalker"/> and <see cref="SafeSyntaxRewriter"/>, and for
    /// <see cref="StackLimits"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The tests run the visitors on threads that they create with a stack of 1 MB. This reproduces the hosts whose threads
    /// have a stack of 1 MB, such as Visual Studio, whatever the stack size of the threads of the test runner is.
    /// </para>
    /// <para>
    /// <see cref="StackLimits"/> queries the operating system and applies the margin of the runtime, so these tests are
    /// platform tests: they run on Windows, Linux and macOS, on .NET Framework and on .NET.
    /// </para>
    /// </remarks>
    public sealed class RecursionGuardTests
    {
        /// <summary>
        /// The stack size of the threads that the tests create.
        /// </summary>
        private const int _threadStackSize = 1024 * 1024;

        /// <summary>
        /// The number of calls in the chain of calls parsed by <see cref="CreateDeepTree"/>. Each call adds two levels of
        /// nesting to the syntax tree, so the depth of the tree is about 2,800, like the aspect test <c>LongCallChain</c>.
        /// This is less than the depth that the guard supports when the bounds of the stack are not known.
        /// </summary>
        private const int _callCount = 1400;

        /// <summary>
        /// The output of the current test.
        /// </summary>
        private readonly ITestOutputHelper _testOutput;

        public RecursionGuardTests( ITestOutputHelper testOutput )
        {
            this._testOutput = testOutput;
        }

        /// <summary>
        /// Gets a value indicating whether <see cref="StackLimits"/> supports the current operating system. On these
        /// operating systems, the tests require <see cref="StackLimits.TryGetAvailableStackSize"/> to succeed, so that a
        /// defect in the native query cannot pass unnoticed through the fallback behavior of <see cref="SafeSyntaxWalker"/>.
        /// </summary>
        private static bool IsStackLimitsSupported
            => RuntimeInformation.IsOSPlatform( OSPlatform.Windows )
               || RuntimeInformation.IsOSPlatform( OSPlatform.Linux )
               || RuntimeInformation.IsOSPlatform( OSPlatform.OSX );

        /// <summary>
        /// Verifies that <see cref="StackLimits"/> returns a plausible value on the current thread and on a thread of the
        /// thread pool, and that it returns a value on every operating system that <see cref="StackLimits"/> supports.
        /// </summary>
        [Fact]
        public async Task StackLimitsReturnsPlausibleValue()
        {
            AssertPlausible( GetAvailableStackSize() );
            AssertPlausible( await Task.Run( GetAvailableStackSize, TestContext.Current.CancellationToken ) );

            static long? GetAvailableStackSize() => StackLimits.TryGetAvailableStackSize( out var available ) ? available : null;

            static void AssertPlausible( long? available )
            {
                if ( IsStackLimitsSupported )
                {
                    Assert.NotNull( available );
                }

                if ( available != null )
                {
                    Assert.InRange( available.Value, 1, 1024L * 1024 * 1024 );
                }
            }
        }

        /// <summary>
        /// Verifies that the available stack size returned by <see cref="StackLimits"/> is close to zero when
        /// <see cref="RuntimeHelpers.EnsureSufficientExecutionStack"/> starts to fail. This detects a change of the margin
        /// that the runtime applies.
        /// </summary>
        [Fact]
        public void AvailableStackSizeMatchesRuntimeCheck()
        {
            if ( !IsStackLimitsSupported )
            {
                // StackLimits does not support this operating system.
                return;
            }

            Assert.True( StackLimits.TryGetAvailableStackSize( out _ ) );

            var availableAtFailure = RunOnThread( 0, FindAvailableStackSizeAtRuntimeFailure );

            this._testOutput.WriteLine( $"Available stack size when the runtime check fails: {availableAtFailure} bytes." );

            Assert.InRange( availableAtFailure, -16 * 1024, 16 * 1024 );
        }

        /// <summary>
        /// Verifies that a <see cref="SafeSyntaxWalker"/> visits every node of a deep syntax tree on a thread with a stack
        /// of 1 MB.
        /// </summary>
        [Fact]
        public void DeepWalkWithSmallStackSucceeds()
        {
            var root = CreateDeepTree();

            var visitedNodeCount = RunOnThread( 0, () => CountNodes( root ) );

            Assert.Equal( root.DescendantNodesAndSelf().Count(), visitedNodeCount );
        }

        /// <summary>
        /// Verifies that a <see cref="SafeSyntaxWalker"/> visits every node of a deep syntax tree when the caller has
        /// already used most of a stack of 1 MB. The guard must then switch to another thread before the fixed depth of
        /// 500 levels at which it used to switch (issue #2077).
        /// </summary>
        [Fact]
        public void DeepWalkWhenCallerHasUsedMostOfItsStackSucceeds()
        {
            var root = CreateDeepTree();

            var visitedNodeCount = RunOnThread( 600 * 1024, () => CountNodes( root ) );

            Assert.Equal( root.DescendantNodesAndSelf().Count(), visitedNodeCount );
        }

        /// <summary>
        /// Verifies that a <see cref="SafeSyntaxRewriter"/> rewrites every node of a deep syntax tree on a thread with a
        /// stack of 1 MB, when the caller has already used most of this stack.
        /// </summary>
        [Fact]
        public void DeepRewriteWhenCallerHasUsedMostOfItsStackSucceeds()
        {
            var root = CreateDeepTree();

            var newRoot = RunOnThread( 600 * 1024, () => new RenameRewriter().Visit( root )! );

            var identifiers = newRoot.DescendantNodes().OfType<IdentifierNameSyntax>().Select( n => n.Identifier.Text ).ToList();

            Assert.Equal( _callCount, identifiers.Count( n => n == RenameRewriter.NewName ) );
            Assert.DoesNotContain( "M", identifiers );
        }

        /// <summary>
        /// Verifies that an exception thrown by a <see cref="SafeSyntaxWalker"/> after the guard has switched to another
        /// thread reaches the caller of <see cref="SafeSyntaxWalker.Visit"/> (issue #2080).
        /// </summary>
        [Fact]
        public void ExceptionAfterSwitchReachesCaller()
        {
            var root = CreateDeepTree();

            var exception = Assert.Throws<SyntaxProcessingException>(
                () => RunOnThread(
                    0,
                    () =>
                    {
                        new ThrowOnThisWalker().Visit( root );

                        return 0;
                    } ) );

            Assert.IsType<InvalidOperationException>( exception.InnerException );
        }

        /// <summary>
        /// Verifies that the message of a <see cref="SyntaxProcessingException"/> can be rendered, on a thread with a stack of
        /// 1 MB, for a node that has both a deep subtree and many ancestors (issue #2083).
        /// </summary>
        [Fact]
        public void MessageOfDeepNodeCanBeRendered()
        {
            var root = CreateDeepTree();

            var message = RunOnThread(
                0,
                () =>
                {
                    try
                    {
                        new ThrowAtDepthWalker( _callCount ).Visit( root );
                    }
                    catch ( SyntaxProcessingException e )
                    {
                        return e.Message;
                    }

                    throw new InvalidOperationException( "The walker did not throw." );
                } );

            Assert.Contains( "while processing the InvocationExpression with code `this.M().M().M()", message, StringComparison.Ordinal );
            Assert.Contains( "...`", message, StringComparison.Ordinal );
        }

        /// <summary>
        /// Measures the stack that <see cref="SafeSyntaxWalker"/> uses for each level of the syntax tree, and writes it to
        /// the test output. The measure is used to choose the available stack size at which the guard switches threads.
        /// </summary>
        [Fact]
        public void MeasureStackUsagePerLevel()
        {
            var root = CreateDeepTree();

            var bytesPerLevel = RunOnThread(
                0,
                () =>
                {
                    var walker = new MeasuringWalker();
                    walker.Visit( root );

                    return walker.BytesPerLevel;
                } );

            this._testOutput.WriteLine( bytesPerLevel == null ? "The bounds of the stack are not known." : $"Stack usage: {bytesPerLevel} bytes per level." );
        }

        /// <summary>
        /// Parses a method body that contains a chain of <see cref="_callCount"/> calls, and returns the root of the tree.
        /// </summary>
        private static SyntaxNode CreateDeepTree()
        {
            var code = new StringBuilder();
            code.Append( "class C { C M() => this; void F() { this" );

            for ( var i = 0; i < _callCount; i++ )
            {
                code.Append( ".M()" );
            }

            code.Append( "; } }" );

            return CSharpSyntaxTree.ParseText( code.ToString() ).GetRoot();
        }

        /// <summary>
        /// Visits the given tree with a <see cref="CountingWalker"/> and returns the number of visited nodes.
        /// </summary>
        private static int CountNodes( SyntaxNode root )
        {
            var walker = new CountingWalker();
            walker.Visit( root );

            return walker.Count;
        }

        /// <summary>
        /// Runs the given function on a new thread with a stack of <see cref="_threadStackSize"/> bytes, after using
        /// <paramref name="stackToConsume"/> bytes of that stack, and returns its result or rethrows its exception.
        /// </summary>
        private static T RunOnThread<T>( int stackToConsume, Func<T> func )
        {
            var result = default(T);
            ExceptionDispatchInfo? exception = null;

            var thread = new Thread(
                () =>
                {
                    try
                    {
                        result = ConsumeStack( stackToConsume, func );
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
        /// Uses at least <paramref name="bytes"/> bytes of stack through recursive calls, then calls the given function.
        /// </summary>
        [MethodImpl( MethodImplOptions.NoInlining )]
        private static unsafe T ConsumeStack<T>( int bytes, Func<T> func )
        {
            if ( bytes <= 0 )
            {
                return func();
            }

            const int frameSize = 4096;
            var buffer = stackalloc byte[frameSize];
            buffer[0] = 1;

            var result = ConsumeStack( bytes - frameSize, func );

            // Reading the buffer after the call prevents the call from being compiled as a tail call.
            Volatile.Read( ref buffer[0] );

            return result;
        }

        /// <summary>
        /// Calls itself recursively until <see cref="RuntimeHelpers.EnsureSufficientExecutionStack"/> throws, and returns
        /// the available stack size reported by <see cref="StackLimits"/> at that point.
        /// </summary>
        [MethodImpl( MethodImplOptions.NoInlining )]
        private static unsafe long FindAvailableStackSizeAtRuntimeFailure()
        {
            StackLimits.TryGetAvailableStackSize( out var available );

            try
            {
                RuntimeHelpers.EnsureSufficientExecutionStack();
            }
            catch ( InsufficientExecutionStackException )
            {
                return available;
            }

            var buffer = stackalloc byte[1024];
            buffer[0] = 1;

            var result = FindAvailableStackSizeAtRuntimeFailure();

            // Reading the buffer after the call prevents the call from being compiled as a tail call.
            Volatile.Read( ref buffer[0] );

            return result;
        }

        /// <summary>
        /// A <see cref="SafeSyntaxWalker"/> that counts the nodes that it visits.
        /// </summary>
        private sealed class CountingWalker : SafeSyntaxWalker
        {
            /// <summary>
            /// Gets the number of visited nodes.
            /// </summary>
            public int Count { get; private set; }

            /// <inheritdoc />
            protected override void VisitCore( SyntaxNode? node )
            {
                if ( node != null )
                {
                    this.Count++;
                }

                base.VisitCore( node );
            }
        }

        /// <summary>
        /// A <see cref="SafeSyntaxRewriter"/> that renames every identifier <c>M</c> to <see cref="NewName"/>.
        /// </summary>
        private sealed class RenameRewriter : SafeSyntaxRewriter
        {
            /// <summary>
            /// The new name of the identifiers named <c>M</c>.
            /// </summary>
            public const string NewName = "N";

            /// <inheritdoc />
            public override SyntaxNode? VisitIdentifierName( IdentifierNameSyntax node )
                => node.Identifier.Text == "M" ? node.WithIdentifier( SyntaxFactory.Identifier( NewName ) ) : node;
        }

        /// <summary>
        /// A <see cref="SafeSyntaxWalker"/> that throws an <see cref="InvalidOperationException"/> when it visits the
        /// <c>this</c> expression, which is the deepest node of the tree created by <see cref="CreateDeepTree"/>.
        /// </summary>
        private sealed class ThrowOnThisWalker : SafeSyntaxWalker
        {
            /// <inheritdoc />
            protected override void VisitCore( SyntaxNode? node )
            {
                if ( node is ThisExpressionSyntax )
                {
                    throw new InvalidOperationException( "Simulated failure after a thread switch." );
                }

                base.VisitCore( node );
            }
        }

        /// <summary>
        /// A <see cref="SafeSyntaxWalker"/> that throws an <see cref="InvalidOperationException"/> when it visits an
        /// invocation at a given depth of the tree.
        /// </summary>
        private sealed class ThrowAtDepthWalker : SafeSyntaxWalker
        {
            private readonly int _throwDepth;
            private int _depth;

            public ThrowAtDepthWalker( int throwDepth )
            {
                this._throwDepth = throwDepth;
            }

            /// <inheritdoc />
            protected override void VisitCore( SyntaxNode? node )
            {
                this._depth++;

                if ( this._depth >= this._throwDepth && node is InvocationExpressionSyntax )
                {
                    throw new InvalidOperationException( "Simulated failure at a given depth." );
                }

                base.VisitCore( node );

                this._depth--;
            }
        }

        /// <summary>
        /// A <see cref="SafeSyntaxWalker"/> that measures the stack used for each level of the tree between the levels
        /// <see cref="_firstLevel"/> and <see cref="_lastLevel"/>.
        /// </summary>
        private sealed class MeasuringWalker : SafeSyntaxWalker
        {
            private const int _firstLevel = 10;
            private const int _lastLevel = 210;

            private int _depth;
            private long? _firstAvailable;

            /// <summary>
            /// Gets the number of bytes of stack used for each level, or <c>null</c> when the bounds of the stack are not known.
            /// </summary>
            public long? BytesPerLevel { get; private set; }

            /// <inheritdoc />
            protected override void VisitCore( SyntaxNode? node )
            {
                this._depth++;

                if ( this._depth is _firstLevel or _lastLevel && StackLimits.TryGetAvailableStackSize( out var available ) )
                {
                    if ( this._depth == _firstLevel )
                    {
                        this._firstAvailable ??= available;
                    }
                    else if ( this.BytesPerLevel == null && this._firstAvailable != null )
                    {
                        this.BytesPerLevel = (this._firstAvailable.Value - available) / (_lastLevel - _firstLevel);
                    }
                }

                base.VisitCore( node );

                this._depth--;
            }
        }
    }
}
