// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.CompileTime.Manifest;
using Metalama.Testing.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CompileTime
{
    public sealed class TemplatingScopeExtensionsTests : UnitTestClass
    {
        /// <summary>
        /// Regression test for https://github.com/metalama/Metalama/issues/701.
        /// Verifies that ToExecutionScope handles all TemplatingScope enum values without throwing.
        /// The design-time pipeline can encounter any scope value when building the template manifest,
        /// particularly when user code contains errors (e.g. invalid using directives). Each scope must
        /// map to a valid ExecutionScope rather than throw an AssertionFailedException.
        /// </summary>
        [Fact]
        public void ToExecutionScope_HandlesAllScopeValues()
        {
            var allValues = Enum.GetValues( typeof(TemplatingScope) ).Cast<TemplatingScope>();
            var failures = new List<string>();

            foreach ( var scope in allValues )
            {
                var exception = Record.Exception( () => scope.ToExecutionScope() );

                if ( exception != null )
                {
                    failures.Add( $"{scope}: {exception.Message}" );
                }
            }

            Assert.Empty( failures );
        }

        /// <summary>
        /// Regression test for https://github.com/metalama/Metalama/issues/701.
        /// Verifies that <see cref="TemplateSymbolManifest.Builder.Build"/> does not throw when
        /// a symbol has been added with a <see cref="TemplatingScope"/> value that was previously
        /// unhandled by <see cref="TemplatingScopeExtensions.ToExecutionScope"/>.
        /// This exercises the exact code path from the stack trace: <c>TemplateSymbolManifest.Builder.Build()</c>
        /// → <c>TemplatingScopeExtensions.ToExecutionScope()</c>.
        /// </summary>
        [Theory]
        [InlineData( nameof(TemplatingScope.LateBound) )]
        [InlineData( nameof(TemplatingScope.MustFollowParent) )]
        [InlineData( nameof(TemplatingScope.ForcedRunTimeOrCompileTime) )]
        [InlineData( nameof(TemplatingScope.ImplicitlyRunTimeOrCompileTime) )]
        [InlineData( nameof(TemplatingScope.NotCompileTimeOnly) )]
        public void ManifestBuild_HandlesAllScopeValues( string scopeName )
        {
            var scope = Enum.Parse<TemplatingScope>( scopeName );

            using var testContext = this.CreateTestContext();

            var compilation = testContext.CreateCSharpCompilation(
                """
                class C
                {
                    void M() {}
                }
                """ );

            var symbol = compilation.GetTypeByMetadataName( "C" )!.GetMembers( "M" ).Single();

            var builder = new TemplateProjectManifestBuilder( compilation.SourceModule.GlobalNamespace );
            builder.AddOrUpdateSymbol( symbol, scope );

            // Build() calls ToExecutionScope() on the stored scope — this must not throw.
            var manifest = builder.Build();

            Assert.NotNull( manifest );
        }
    }
}
