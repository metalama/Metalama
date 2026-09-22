// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.AdviceImpl.Introduction;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Testing.UnitTesting;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.CodeModel;

/// <summary>
/// Tests the enumeration of the declarations derived from a member introduced by an aspect.
/// </summary>
/// <remarks>
/// Regression test for https://github.com/metalama/Metalama/issues/2048. The declaring type of an introduced member
/// is stored as a reference in the builder data, and a facade of that builder data is read in a consuming
/// compilation model that is not necessarily the compilation model that produced the builder. Reading
/// <c>DeclaringType</c> on such a facade resolves the reference with <c>throwIfMissing: true</c> and raises
/// <c>SymbolNotFoundException</c>. The design-time pipeline reached that property from the inherited-aspect source,
/// through <c>SourceMember.GetDerivedDeclarationsCore</c>, and the exception terminated the whole pipeline.
/// </remarks>
public sealed class IntroducedMemberDerivedDeclarationsTests : UnitTestClass
{
    public IntroducedMemberDerivedDeclarationsTests( ITestOutputHelper logger ) : base( logger ) { }

    /// <summary>
    /// Verifies that the declaring type of an introduced member resolves, and that the derived declarations are
    /// enumerated, in the compilation that produced the introduction.
    /// </summary>
    /// <remarks>
    /// This is the case the guard added for issue #2048 must not affect. The enumeration returns nothing because no
    /// declaration of the compilation overrides a member that an aspect introduces, and what matters here is that the
    /// declaring type is read without error.
    /// </remarks>
    [Fact]
    public void DeclaringTypeResolvesInTheProducingCompilation()
    {
        using var testContext = this.CreateTestContext();

        var introducedMethod = IntroduceVirtualMethod( testContext.CreateCompilationModel( _code ) );

        Assert.Equal( "BaseClass", introducedMethod.DeclaringType.Name );

        var derivedDeclarations = ((IDeclarationImpl) introducedMethod).GetDerivedDeclarations().ToList();

        Assert.Empty( derivedDeclarations );
    }

    /// <summary>
    /// Verifies that the derived declarations of an introduced member are enumerated as an empty sequence, instead of
    /// raising <see cref="Metalama.Framework.Engine.CodeModel.SymbolNotFoundException"/>, when the declaring type of
    /// that member is absent from the compilation the member is read in.
    /// </summary>
    [Fact]
    public void DerivedDeclarationsAreEmptyWhenTheDeclaringTypeIsAbsentFromTheCompilation()
    {
        using var testContext = this.CreateTestContext();

        var introducedMethod = IntroduceVirtualMethod( testContext.CreateCompilationModel( _code ) );

        // A compilation in which the declaring type of the introduced member does not exist.
        var otherCompilation = testContext.CreateCompilationModel( _codeWithoutBaseClass );

        var foreignFacade = introducedMethod.ToRef().GetTarget( otherCompilation );

        var derivedDeclarations = ((IDeclarationImpl) foreignFacade).GetDerivedDeclarations().ToList();

        Assert.Empty( derivedDeclarations );
    }

    private const string _code = """
                                 public class BaseClass { }

                                 public class DerivedClass : BaseClass
                                 {
                                     public virtual void Method() { }
                                 }
                                 """;

    private const string _codeWithoutBaseClass = """
                                                 public class SomethingElse { }
                                                 """;

    /// <summary>
    /// Adds a virtual method named <c>Method</c> to <c>BaseClass</c> and returns the introduced member.
    /// </summary>
    /// <remarks>
    /// The method is virtual because <c>IntroducedMember.GetDerivedDeclarations</c> returns nothing for a member that
    /// cannot be inherited, and the declaring type is the base type of <c>DerivedClass</c> so that the enumeration
    /// has a derived type to walk.
    /// </remarks>
    private static IMethod IntroduceVirtualMethod( CompilationModel compilation )
    {
        var mutableCompilation = compilation.CreateMutableClone();
        var baseClass = mutableCompilation.Types.OfName( "BaseClass" ).Single();

        // The advice is null because this test builds the declaration directly rather than through an aspect, as the
        // other tests of the code model do. No member read here goes through the advice.
        var methodBuilder = new MethodBuilder( null!, baseClass, "Method" ) { IsVirtual = true };
        methodBuilder.Freeze();
        mutableCompilation.AddTransformation( methodBuilder.ToTransformation() );

        return mutableCompilation.Types.OfName( "BaseClass" ).Single().Methods.OfName( "Method" ).Single();
    }
}
