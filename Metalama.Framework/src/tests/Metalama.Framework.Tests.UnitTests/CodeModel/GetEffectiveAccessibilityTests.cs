// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Testing.UnitTesting;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CodeModel;

public sealed class GetEffectiveAccessibilityTests : UnitTestClass
{
    [Fact]
    public void PublicClass_PublicMethod()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "public class C { public void M() {} }" );
        var method = compilation.Types.OfName( "C" ).Single().Methods.OfName( "M" ).Single();

        Assert.Equal( Accessibility.Public, method.GetEffectiveAccessibility() );
    }

    [Fact]
    public void InternalClass_PublicMethod()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "internal class C { public void M() {} }" );
        var method = compilation.Types.OfName( "C" ).Single().Methods.OfName( "M" ).Single();

        Assert.Equal( Accessibility.Internal, method.GetEffectiveAccessibility() );
    }

    [Fact]
    public void PublicClass_InternalMethod()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "public class C { internal void M() {} }" );
        var method = compilation.Types.OfName( "C" ).Single().Methods.OfName( "M" ).Single();

        Assert.Equal( Accessibility.Internal, method.GetEffectiveAccessibility() );
    }

    [Fact]
    public void NestedType_PrivateInPublic()
    {
        using var testContext = this.CreateTestContext();

        const string code = "public class Outer { private class Inner { public void M() {} } }";
        var compilation = testContext.CreateCompilationModel( code );
        var inner = compilation.Types.OfName( "Outer" ).Single().Types.OfName( "Inner" ).Single();

        Assert.Equal( Accessibility.Private, inner.GetEffectiveAccessibility() );
        Assert.Equal( Accessibility.Private, inner.Methods.OfName( "M" ).Single().GetEffectiveAccessibility() );
    }

    [Fact]
    public void TopLevelInternalClass()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "internal class C {}" );
        var type = compilation.Types.OfName( "C" ).Single();

        Assert.Equal( Accessibility.Internal, type.GetEffectiveAccessibility() );
    }

    [Fact]
    public void Type_PublicNamedType()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "public class C {}" );
        var type = (IType) compilation.Types.OfName( "C" ).Single();

        Assert.Equal( Accessibility.Public, type.GetEffectiveAccessibility() );
    }

    [Fact]
    public void Type_InternalNamedType()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "internal class C {}" );
        var type = (IType) compilation.Types.OfName( "C" ).Single();

        Assert.Equal( Accessibility.Internal, type.GetEffectiveAccessibility() );
    }

    [Fact]
    public void Type_ArrayOfInternalType()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "internal class C { public C[] Items; }" );
        var field = compilation.Types.OfName( "C" ).Single().Fields.OfName( "Items" ).Single();

        Assert.Equal( Accessibility.Internal, field.Type.GetEffectiveAccessibility() );
    }

    [Fact]
    public void Type_ArrayOfPublicType()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "public class C { public C[] Items; }" );
        var field = compilation.Types.OfName( "C" ).Single().Fields.OfName( "Items" ).Single();

        Assert.Equal( Accessibility.Public, field.Type.GetEffectiveAccessibility() );
    }

    [Fact]
    public void Type_GenericWithInternalArgument()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
using System.Collections.Generic;
internal class InternalClass {}
public class C { public IList<InternalClass> Items; }
";
        var compilation = testContext.CreateCompilationModel( code, ignoreErrors: true );
        var field = compilation.Types.OfName( "C" ).Single().Fields.OfName( "Items" ).Single();

        Assert.Equal( Accessibility.Internal, field.Type.GetEffectiveAccessibility() );
    }

    [Fact]
    public void Type_GenericWithAllPublicArguments()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
using System.Collections.Generic;
public class C { public IList<int> Items; }
";
        var compilation = testContext.CreateCompilationModel( code );
        var field = compilation.Types.OfName( "C" ).Single().Fields.OfName( "Items" ).Single();

        Assert.Equal( Accessibility.Public, field.Type.GetEffectiveAccessibility() );
    }

    [Fact]
    public void Type_NestedGenericWithInternalArgument()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
using System.Collections.Generic;
internal class InternalClass {}
public class C { public IDictionary<string, IList<InternalClass>> Items; }
";
        var compilation = testContext.CreateCompilationModel( code, ignoreErrors: true );
        var field = compilation.Types.OfName( "C" ).Single().Fields.OfName( "Items" ).Single();

        Assert.Equal( Accessibility.Internal, field.Type.GetEffectiveAccessibility() );
    }

    [Fact]
    public void Type_Dynamic()
    {
        using var testContext = this.CreateTestContext();

        const string code = "public class C { public dynamic Value; }";
        var compilation = testContext.CreateCompilationModel( code );
        var field = compilation.Types.OfName( "C" ).Single().Fields.OfName( "Value" ).Single();

        Assert.Equal( Accessibility.Public, field.Type.GetEffectiveAccessibility() );
    }
}
