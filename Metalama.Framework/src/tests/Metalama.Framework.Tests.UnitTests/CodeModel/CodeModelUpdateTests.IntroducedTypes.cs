// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.AdviceImpl.InterfaceImplementation;
using Metalama.Framework.Engine.AdviceImpl.Introduction;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.References;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CodeModel;

public sealed partial class CodeModelUpdateTests
{
    [Fact]
    public void AddTypeToCompilation_VisibleInCompilationTypesCollection()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "class Outer;" ).CreateMutableClone();

        var type = new NamedTypeBuilder( null!, compilation.GlobalNamespace, "C", TypeKind.Class );
        type.Freeze();
        compilation.AddTransformation( type.CreateTransformation() );

        Assert.Single( compilation.GlobalNamespace.Types.OfName( "C" ) );
        Assert.Single( compilation.Types.OfName( "C" ) );

        var nestedType = new NamedTypeBuilder( null!, compilation.Types.OfName( "Outer" ).Single(), "Inner", TypeKind.Class );
        nestedType.Freeze();
        compilation.AddTransformation( nestedType.CreateTransformation() );

        Assert.Single( compilation.GlobalNamespace.Types.OfName( "Outer" ).Single().Types );
        Assert.Single( compilation.AllTypes.OfName( "Inner" ) );
    }

    [Fact]
    public void AddTypeToIntroducedNamespace_IsVisibleInThatNamespace()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var nsBuilder = new NamespaceBuilder( null!, compilation.GlobalNamespace, "NS" );
        nsBuilder.Freeze();
        compilation.AddTransformation( nsBuilder.CreateTransformation() );

        var typeBuilder = new NamedTypeBuilder( null!, nsBuilder, "C", TypeKind.Class );
        typeBuilder.Freeze();
        compilation.AddTransformation( typeBuilder.CreateTransformation() );

        var ns = compilation.GlobalNamespace.GetDescendant( "NS" );

        Assert.NotNull( ns );

        Assert.Single( ns.Types.OfName( "C" ) );
        Assert.Single( compilation.Types.OfName( "C" ) );
    }

    [Fact]
    public void AddMethodToEmptyIntroducedType_InitializeBefore_Complete()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
class C
{    
}";

        var immutableCompilation1 = testContext.CreateCompilationModel( code );
        Assert.Empty( immutableCompilation1.Types.Single().Methods );

        var mutableCompilation1 = immutableCompilation1.CreateMutableClone();

        var type1 = Assert.Single( mutableCompilation1.Types );

        // Add a type.
        var typeBuilder = new NamedTypeBuilder( null!, type1, "T", TypeKind.Class );
        typeBuilder.Freeze();
        mutableCompilation1.AddTransformation( typeBuilder.CreateTransformation() );

        var immutableCompilation2 = mutableCompilation1.CreateImmutableClone();
        var mutableCompilation2 = immutableCompilation2.CreateMutableClone();

        var type2 = Assert.Single( mutableCompilation2.Types );
        var nestedType = Assert.Single( type2.Types );

        // Instantiate the memoize the collection of methods of the nested type.
        Assert.Empty( nestedType.Methods );

        // Add a method.
        var methodBuilder = new MethodBuilder( null!, nestedType, "M" );
        methodBuilder.Freeze();
        mutableCompilation2.AddTransformation( methodBuilder.ToTransformation() );

        // Assert that the method has been added.
        Assert.Single( nestedType.Methods );

        // Assert that there is still no method in original compilation.
        Assert.Empty( immutableCompilation2.Types.Single().Types.Single().Methods );
    }

    [Fact]
    public void AddMethodToEmptyIntroducedType_InitializeBefore_ByName()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
class C
{    
}";

        var immutableCompilation1 = testContext.CreateCompilationModel( code );
        Assert.Empty( immutableCompilation1.Types.Single().Methods );

        var mutableCompilation1 = immutableCompilation1.CreateMutableClone();

        var type1 = Assert.Single( mutableCompilation1.Types );

        // Add a type.
        var typeBuilder = new NamedTypeBuilder( null!, type1, "T", TypeKind.Class );
        typeBuilder.Freeze();
        mutableCompilation1.AddTransformation( typeBuilder.CreateTransformation() );

        var immutableCompilation2 = mutableCompilation1.CreateImmutableClone();
        var mutableCompilation2 = immutableCompilation2.CreateMutableClone();

        var type2 = Assert.Single( mutableCompilation2.Types );
        var nestedType = Assert.Single( type2.Types );

        // Instantiate the memoize the collection of methods of the nested type.
        Assert.Empty( nestedType.Methods.OfName( "M" ) );

        // Add a method.
        var methodBuilder = new MethodBuilder( null!, nestedType, "M" );
        methodBuilder.Freeze();
        mutableCompilation2.AddTransformation( methodBuilder.ToTransformation() );

        // Assert that the method has been added.
        Assert.Single( nestedType.Methods.OfName( "M" ) );

        // Assert that there is still no method in original compilation.
        Assert.Empty( immutableCompilation2.Types.Single().Types.Single().Methods.OfName( "M" ) );
    }

    [Fact]
    public void AddMethodToEmptyIntroducedType_DontInitializeBefore_Complete()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
class C
{    
}";

        var immutableCompilation1 = testContext.CreateCompilationModel( code );
        Assert.Empty( immutableCompilation1.Types.Single().Methods );

        var mutableCompilation1 = immutableCompilation1.CreateMutableClone();

        var type1 = Assert.Single( mutableCompilation1.Types );

        // Add a type.
        var typeBuilder = new NamedTypeBuilder( null!, type1, "T", TypeKind.Class );
        typeBuilder.Freeze();
        mutableCompilation1.AddTransformation( typeBuilder.CreateTransformation() );

        var immutableCompilation2 = mutableCompilation1.CreateImmutableClone();
        var mutableCompilation2 = immutableCompilation2.CreateMutableClone();

        var type2 = Assert.Single( mutableCompilation2.Types );
        var nestedType = Assert.Single( type2.Types );

        // Add a method.
        var methodBuilder = new MethodBuilder( null!, nestedType, "M" );
        methodBuilder.Freeze();
        mutableCompilation2.AddTransformation( methodBuilder.ToTransformation() );

        // Assert that the method has been added.
        Assert.Single( nestedType.Methods );

        // Assert that there is still no method in original compilation.
        Assert.Empty( immutableCompilation2.Types.Single().Types.Single().Methods );
    }

    [Fact]
    public void AddMethodToEmptyIntroducedType_DontInitializeBefore_ByName()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
class C
{    
}";

        var immutableCompilation1 = testContext.CreateCompilationModel( code );
        Assert.Empty( immutableCompilation1.Types.Single().Methods );

        var mutableCompilation1 = immutableCompilation1.CreateMutableClone();

        var type1 = Assert.Single( mutableCompilation1.Types );

        // Add a type.
        var typeBuilder = new NamedTypeBuilder( null!, type1, "T", TypeKind.Class );
        typeBuilder.Freeze();
        mutableCompilation1.AddTransformation( typeBuilder.CreateTransformation() );

        var immutableCompilation2 = mutableCompilation1.CreateImmutableClone();
        var mutableCompilation2 = immutableCompilation2.CreateMutableClone();

        var type2 = Assert.Single( mutableCompilation2.Types );
        var nestedType = Assert.Single( type2.Types );

        // Add a method.
        var methodBuilder = new MethodBuilder( null!, nestedType, "M" );
        methodBuilder.Freeze();
        mutableCompilation2.AddTransformation( methodBuilder.ToTransformation() );

        // Assert that the method has been added.
        Assert.Single( nestedType.Methods.OfName( "M" ) );

        // Assert that there is still no method in original compilation.
        Assert.Empty( immutableCompilation2.Types.Single().Types.Single().Methods.OfName( "M" ) );
    }

    [Fact]
    public void DerivedTypes()
    {
        using var testContext = this.CreateTestContext();

        const string code =
            """
            interface I;

            class Target;
            """;

        var initialCompilation = testContext.CreateCompilationModel( code );

        var target = initialCompilation.Types.OfName( "Target" ).Single();

        var baseType = new NamedTypeBuilder( null!, target, "B", TypeKind.Class );
        baseType.Freeze();

        var derivedType = new NamedTypeBuilder( null!, target, "C", TypeKind.Class ) { BaseType = baseType };
        derivedType.Freeze();

        var interfaceType = initialCompilation.Types.OfName( "I" ).Single();

        var implementInterface = new IntroduceInterfaceTransformation( null!, derivedType.ToFullRef<INamedType>(), interfaceType.ToFullRef(), [] );

        var finalCompilation = initialCompilation.WithTransformations(
            [baseType.CreateTransformation(), derivedType.CreateTransformation(), implementInterface] );

        var baseClass = finalCompilation.Types.OfName( "Target" ).Single().Types.OfName( "B" ).Single();

        var derivedClass = Assert.Single( finalCompilation.GetDerivedTypes( baseClass ) );

        Assert.Equal( "C", derivedClass.Name );

        var implementedInterface = finalCompilation.Types.OfName( "I" ).Single();

        var implementingClass = Assert.Single( finalCompilation.GetDerivedTypes( implementedInterface ) );

        Assert.Same( derivedClass, implementingClass );
    }

    [Fact]
    public void IntroducedTypeAsTypeArgument()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
using System;

class B<TB>
{
   public virtual TB BaseMethod() => default;
   public virtual TB BaseMethod2() => default; 
}

class C<TC> : B<TC>
{    
    TC field;
    TC Property { get; set; }
    TC Method( TC p1, TC[] p2, Action<TC> p3 ) => p1;
    event Action<TC> Event;
   public override TC BaseMethod() => base.BaseMethod();
}";

        var immutableCompilation1 = testContext.CreateCompilationModel( code );

        // Add a type.
        var typeBuilder = new NamedTypeBuilder( null!, immutableCompilation1.GlobalNamespace, "Introduced", TypeKind.Class );
        typeBuilder.Freeze();

        var finalCompilation = immutableCompilation1.WithTransformations( [typeBuilder.CreateTransformation()] );

        var genericClass = finalCompilation.Types.OfName( "C" ).Single();
        var introducedClass = typeBuilder.ForCompilation<INamedType>( finalCompilation );

        var genericClassInstance = genericClass.WithTypeArguments( introducedClass );

        Assert.Equal( "C<Introduced>", genericClassInstance.ToString() );
        Assert.Equal( "B<Introduced>", genericClassInstance.BaseType!.ToString() );
        Assert.Equal( introducedClass, genericClassInstance.Fields.OfName( "field" ).Single().Type );
        Assert.Equal( introducedClass, genericClassInstance.Properties.Single().Type );
        var method = genericClassInstance.Methods.OfName( "Method" ).Single();
        Assert.Equal( introducedClass, method.ReturnType );
        Assert.Equal( introducedClass, method.Parameters[0].Type );
        Assert.Equal( "Introduced[]", method.Parameters[1].Type.ToString() );
        Assert.Equal( "Action<Introduced>", method.Parameters[2].Type.ToString() );

        Assert.Contains( "B<Introduced>.BaseMethod2()", genericClassInstance.AllMethods.SelectAsArray( m => m.ToString() ) );

        var overridingMethod = genericClassInstance.Methods.OfName( "BaseMethod" ).Single();
        Assert.Equal( "B<Introduced>.BaseMethod()", overridingMethod.OverriddenMethod.ToString() );
    }
}