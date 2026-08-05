// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Utilities
{
    /// <summary>
    /// Tests for <see cref="Metalama.Framework.Engine.Utilities.Comparers.SafeSymbolComparer"/> and for the
    /// <see cref="SymbolHelpers.BelongsToCompilation"/> invariant on which it relies.
    /// </summary>
    public sealed class SafeSymbolComparerTests : UnitTestClass
    {
        /// <summary>
        /// Verifies that <see cref="SymbolHelpers.BelongsToCompilation"/> takes no decision for an error symbol,
        /// even when the assembly that contains that symbol has an identity that the current compilation also uses.
        /// </summary>
        /// <remarks>
        /// An error symbol is a placeholder that Roslyn synthesizes for a reference that it could not resolve, so
        /// it is not a legitimate member of any compilation. Reporting it as foreign made the debug-only invariant
        /// of <see cref="Metalama.Framework.Engine.Utilities.Comparers.SafeSymbolComparer"/> throw while the
        /// <see cref="Metalama.Framework.Engine.CompileTime.SymbolClassifier"/> walked the base types of a type
        /// whose references were momentarily incomplete. See issue #1823.
        /// </remarks>
        [Fact]
        public void ErrorSymbolNeverBelongsToADifferentCompilation()
        {
            using var testContext = this.CreateTestContext();

            // Two distinct compilations that share a single assembly identity. Their assembly symbols are therefore
            // distinct instances that the identity lookup of BelongsToCompilation cannot tell apart.
            var compilationWithError = testContext.CreateCSharpCompilation(
                "class C : MissingBase { }",
                ignoreErrors: true,
                assemblyName: "SharedAssemblyName" );

            var otherCompilation = testContext.CreateCSharpCompilation(
                "class D { }",
                ignoreErrors: true,
                assemblyName: "SharedAssemblyName" );

            var errorSymbol = compilationWithError.Assembly.GetTypeByMetadataName( "C" ).AssertNotNull().BaseType.AssertNotNull();
            Assert.Equal( SymbolKind.ErrorType, errorSymbol.Kind );

            Assert.Equal(
                compilationWithError.Assembly.Identity,
                otherCompilation.Assembly.Identity );

            var otherCompilationContext = testContext.ServiceProvider
                .GetRequiredService<ClassifyingCompilationContextFactory>()
                .GetInstance( otherCompilation )
                .CompilationContext;

            Assert.Null( errorSymbol.BelongsToCompilation( otherCompilationContext ) );

            // This is the call that used to throw an AssertionFailedException in debug builds.
            var otherSymbol = otherCompilation.Assembly.GetTypeByMetadataName( "D" ).AssertNotNull();
            Assert.False( otherCompilationContext.SymbolComparer.Equals( errorSymbol, otherSymbol ) );
        }

        /// <summary>
        /// Verifies that the <see cref="Metalama.Framework.Engine.CompileTime.SymbolClassifier"/> classifies a type
        /// whose base type could not be resolved without throwing, which is the scenario reported in issue #1823.
        /// </summary>
        [Fact]
        public void SymbolClassifierAcceptsUnresolvedBaseType()
        {
            using var testContext = this.CreateTestContext();

            var compilation = testContext.CreateCSharpCompilation(
                "class C : MissingBase { }",
                ignoreErrors: true );

            var type = compilation.Assembly.GetTypeByMetadataName( "C" ).AssertNotNull();

            var classifyingContext = testContext.ServiceProvider
                .GetRequiredService<ClassifyingCompilationContextFactory>()
                .GetInstance( compilation );

            Assert.Equal( TemplatingScope.RunTimeOnly, classifyingContext.SymbolClassifier.GetTemplatingScope( type ) );
        }
    }
}
