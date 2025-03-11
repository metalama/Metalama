// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Engine.AdviceImpl.Attributes;
using Metalama.Framework.Engine.AdviceImpl.Introduction;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Testing.UnitTesting;
using System;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CodeModel;

public sealed partial class CodeModelUpdateTests : UnitTestClass
{
    [Fact]
    public void AddMethodToEmptyType_InitializeBefore_Complete()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
class C
{    
}";

        var immutableCompilation = testContext.CreateCompilationModel( code );
        Assert.Empty( immutableCompilation.Types.Single().Methods );

        var compilation = immutableCompilation.CreateMutableClone();

        var type = Assert.Single( compilation.Types );

        // Instantiate the memoize the collection of methods.
        Assert.Empty( type.Methods );

        // Add a method.
        var methodBuilder = new MethodBuilder( null!, type, "M" );
        methodBuilder.Freeze();
        compilation.AddTransformation( methodBuilder.ToTransformation() );

        // Assert that the method has been added.
        Assert.Single( type.Methods );

        // Assert that there is still no method in original compilation.
        Assert.Empty( immutableCompilation.Types.Single().Methods );
    }

    [Fact]
    public void AddMethodToEmptyType_InitializeBefore_ByName()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
class C
{    
}";

        var immutableCompilation = testContext.CreateCompilationModel( code );
        Assert.Empty( immutableCompilation.Types.Single().Methods.OfName( "M" ) );

        var compilation = immutableCompilation.CreateMutableClone();

        var type = Assert.Single( compilation.Types );

        // Instantiate the memoize the collection of methods.
        Assert.Empty( type.Methods.OfName( "M" ) );

        // Add a method.
        var methodBuilder = new MethodBuilder( null!, type, "M" );
        methodBuilder.Freeze();
        compilation.AddTransformation( methodBuilder.ToTransformation() );

        // Assert that the method has been added.
        Assert.Single( type.Methods.OfName( "M" ) );

        // Assert that there is still no method in original compilation.
        Assert.Empty( immutableCompilation.Types.Single().Methods.OfName( "M" ) );
    }

    [Fact]
    public void AddMethodToEmptyType_DontInitializeBefore_Complete()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
class C
{    
}";

        var immutableCompilation = testContext.CreateCompilationModel( code );
        var compilation = immutableCompilation.CreateMutableClone();

        var type = Assert.Single( compilation.Types );

        // Add a method.
        var methodBuilder = new MethodBuilder( null!, type, "M" );
        methodBuilder.Freeze();
        compilation.AddTransformation( methodBuilder.ToTransformation() );

        // Assert that the method has been added.
        Assert.Single( type.Methods );

        // Assert that there is still no method in original compilation.
        Assert.Empty( immutableCompilation.Types.Single().Methods );
    }

    [Fact]
    public void AddMethodToEmptyType_DontInitializeBefore_ByName()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
class C
{    
}";

        var immutableCompilation = testContext.CreateCompilationModel( code );
        var compilation = immutableCompilation.CreateMutableClone();

        var type = Assert.Single( compilation.Types );

        // Add a method.
        var methodBuilder = new MethodBuilder( null!, type, "M" );
        methodBuilder.Freeze();
        compilation.AddTransformation( methodBuilder.ToTransformation() );

        // Assert that the method has been added.
        Assert.Single( type.Methods.OfName( "M" ) );

        // Assert that there is still no method in original compilation.
        Assert.Empty( immutableCompilation.Types.Single().Methods.OfName( "M" ) );
    }

    [Fact]
    public void NonEmptyType_GetByName_Then_GetAll()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
class C
{    
  void M() {}
}";

        var immutableCompilation = testContext.CreateCompilationModel( code );
        var compilation = immutableCompilation.CreateMutableClone();

        var type = Assert.Single( compilation.Types );

        // Assert that the method has been added.
        Assert.Single( type.Methods.OfName( "M" ) );
        Assert.Single( type.Methods );
    }

    [Fact]
    public void AddMethodToNonEmptyType_DontInitializeBefore_ByName()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
class C
{    
void M(int p){}
}";

        var immutableCompilation = testContext.CreateCompilationModel( code );
        var compilation = immutableCompilation.CreateMutableClone();

        var type = Assert.Single( compilation.Types );

        // Add a method.
        var methodBuilder = new MethodBuilder( null!, type, "M" );
        methodBuilder.Freeze();
        compilation.AddTransformation( methodBuilder.ToTransformation() );

        // Assert that the method has been added.
        Assert.Equal( 2, type.Methods.OfName( "M" ).Count() );
    }

    [Fact]
    public void AddMethodToNonEmptyType_InitializeBefore_ByName()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
class C
{    
void M(int p){}
}";

        var immutableCompilation = testContext.CreateCompilationModel( code );
        var compilation = immutableCompilation.CreateMutableClone();

        var type = Assert.Single( compilation.Types );

        // Instantiate the memoize the collection of methods.
        Assert.Single( type.Methods.OfName( "M" ) );

        // Add a method.
        var methodBuilder = new MethodBuilder( null!, type, "M" );
        methodBuilder.Freeze();
        compilation.AddTransformation( methodBuilder.ToTransformation() );

        // Assert that the method has been added.
        Assert.Equal( 2, type.Methods.OfName( "M" ).Count() );
    }

    [Fact]
    public void AddFieldToEmptyType_InitializeBefore_Complete()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
class C
{    
}";

        var immutableCompilation = testContext.CreateCompilationModel( code );
        var compilation = immutableCompilation.CreateMutableClone();

        var type = Assert.Single( compilation.Types );

        // Instantiate the memoize the collection of fields.
        Assert.Empty( type.Fields );

        // Add a field.
        var fieldBuilder = new FieldBuilder( null!, type, "F" );
        fieldBuilder.Freeze();
        compilation.AddTransformation( fieldBuilder.CreateTransformation() );

        // Assert that the method has been added.
        Assert.Single( type.Fields );
    }

    [Fact]
    public void AddFieldToEmptyType_DontInitializeBefore_Complete()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
class C
{    
}";

        var immutableCompilation = testContext.CreateCompilationModel( code );
        var compilation = immutableCompilation.CreateMutableClone();

        var type = Assert.Single( compilation.Types );

        // Add a field.
        var fieldBuilder = new FieldBuilder( null!, type, "F" );
        fieldBuilder.Freeze();
        compilation.AddTransformation( fieldBuilder.CreateTransformation() );

        // Assert that the method has been added.
        Assert.Single( type.Fields );
    }

    [Fact]
    public void AddFieldToEmptyType_InitializeBefore_ByName()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
class C
{    
}";

        var immutableCompilation = testContext.CreateCompilationModel( code );
        var compilation = immutableCompilation.CreateMutableClone();

        var type = Assert.Single( compilation.Types );

        // Instantiate the memoize the collection of fields.
        Assert.Empty( type.Fields.OfName( "F" ) );

        // Add a field.
        var fieldBuilder = new FieldBuilder( null!, type, "F" );
        fieldBuilder.Freeze();
        compilation.AddTransformation( fieldBuilder.CreateTransformation() );

        // Assert that the method has been added.
        Assert.Single( type.Fields.OfName( "F" ) );
    }

    [Fact]
    public void AddFieldToEmptyType_DontInitializeBefore_ByName()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
class C
{    
}";

        var immutableCompilation = testContext.CreateCompilationModel( code );
        var compilation = immutableCompilation.CreateMutableClone();

        var type = Assert.Single( compilation.Types );

        // Add a field.
        var fieldBuilder = new FieldBuilder( null!, type, "F" );
        fieldBuilder.Freeze();
        compilation.AddTransformation( fieldBuilder.CreateTransformation() );

        // Assert that the method has been added.
        Assert.Single( type.Fields.OfName( "F" ) );
    }

    [Fact]
    public void AddParameterToExplicitConstructor()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
class C
{    
   public C() {}
}";

        var immutableCompilation = testContext.CreateCompilationModel( code );
        var compilation = immutableCompilation.CreateMutableClone();

        var constructor = compilation.Types.Single().Constructors.Single();

        // Add a field.
        var parameterBuilder = new ParameterBuilder( constructor, 0, "p", compilation.Factory.GetTypeByReflectionType( typeof(int) ), RefKind.In, null! );
        parameterBuilder.Freeze();
        compilation.AddTransformation( new IntroduceParameterTransformation( null!, parameterBuilder.BuilderData ) );

        Assert.Single( constructor.Parameters );
    }

    [Fact]
    public void AddParameterToImplicitConstructor()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
class C
{    
  
}";

        var immutableCompilation = testContext.CreateCompilationModel( code );
        var compilation = immutableCompilation.CreateMutableClone();

        var constructor = compilation.Types.Single().Constructors.Single();

        var aspectLayerInstance = AspectLayerInstance.CreateTestInstance( immutableCompilation );

        // Add a field.

        var parameterBuilder = new ParameterBuilder(
            constructor,
            0,
            "p",
            compilation.Factory.GetTypeByReflectionType( typeof(int) ),
            RefKind.In,
            aspectLayerInstance );

        parameterBuilder.Freeze();
        compilation.AddTransformation( new IntroduceParameterTransformation( aspectLayerInstance, parameterBuilder.BuilderData ) );

        Assert.Single( constructor.Parameters );
    }

    [Fact]
    public void AddAttribute_Type()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
class C
{    
  
}";

        var immutableCompilation = testContext.CreateCompilationModel( code );
        var compilation = immutableCompilation.CreateMutableClone();
        var type = compilation.Types.Single();

        Assert.Empty( type.Attributes );

        var attributeBuilder = new AttributeBuilder(
            null!,
            type,
            AttributeConstruction.Create( (INamedType) compilation.Factory.GetTypeByReflectionType( typeof(SerializableAttribute) ) ) );

        attributeBuilder.Freeze();

        compilation.AddTransformation( attributeBuilder.CreateTransformation() );

        Assert.Single( type.Attributes );
    }

    [Fact]
    public void AddAttribute_Compilation()
    {
        using var testContext = this.CreateTestContext();

        const string code = "";

        var immutableCompilation = testContext.CreateCompilationModel( code );
        var compilation = immutableCompilation.CreateMutableClone();

        Assert.Empty( compilation.Attributes );

        var aspectLayerInstance = AspectLayerInstance.CreateTestInstance( immutableCompilation );

        var attributeBuilder = new AttributeBuilder(
            aspectLayerInstance,
            compilation,
            AttributeConstruction.Create( (INamedType) compilation.Factory.GetTypeByReflectionType( typeof(SerializableAttribute) ) ) );

        attributeBuilder.Freeze();

        compilation.AddTransformation( attributeBuilder.CreateTransformation() );

        Assert.Single( compilation.Attributes );
    }

    [Fact]
    public void RemoveAttribute_Type()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
[System.Serializable]
class C
{    
  
}";

        var immutableCompilation = testContext.CreateCompilationModel( code );
        var compilation = immutableCompilation.CreateMutableClone();
        var type = compilation.Types.Single();

        Assert.Single( type.Attributes );

        compilation.AddTransformation(
            new RemoveAttributesTransformation(
                null!,
                type.ToFullRef(),
                compilation.Factory.GetTypeByReflectionType( typeof(SerializableAttribute) ).ToRef().AsFullRef<INamedType>() ) );

        Assert.Empty( type.Attributes );
    }

    [Fact]
    public void AddNestedTypeToEmptyType_InitializeBefore_Complete()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
class C
{    
}";

        var immutableCompilation = testContext.CreateCompilationModel( code );
        Assert.Empty( immutableCompilation.Types.Single().Types );

        var compilation = immutableCompilation.CreateMutableClone();

        var type = Assert.Single( compilation.Types );

        // Instantiate the memoize the collection of nested types.
        Assert.Empty( type.Types );

        // Add a nested type.
        var typeBuilder = new NamedTypeBuilder( null!, type, "T", TypeKind.Class );
        typeBuilder.Freeze();
        compilation.AddTransformation( typeBuilder.CreateTransformation() );

        // Assert that the type has been added.
        Assert.Single( type.Types );

        // Assert that there is still no type in original compilation.
        Assert.Empty( immutableCompilation.Types.Single().Types );
    }

    [Fact]
    public void AddNestedTypeToEmptyType_InitializeBefore_OfName()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
class C
{    
}";

        var immutableCompilation = testContext.CreateCompilationModel( code );
        Assert.Empty( immutableCompilation.Types.Single().Types.OfName( "T" ) );

        var compilation = immutableCompilation.CreateMutableClone();

        var type = Assert.Single( compilation.Types );

        // Instantiate the memoize the collection of nested types.
        Assert.Empty( type.Types.OfName( "T" ) );

        // Add a nested type.
        var typeBuilder = new NamedTypeBuilder( null!, type, "T", TypeKind.Class );
        typeBuilder.Freeze();
        compilation.AddTransformation( typeBuilder.CreateTransformation() );

        // Assert that the type has been added.
        Assert.Single( type.Types.OfName( "T" ) );

        // Assert that there is still no type in original compilation.
        Assert.Empty( immutableCompilation.Types.Single().Types.OfName( "T" ) );
    }

    [Fact]
    public void AddNestedTypeToEmptyType_DontInitializeBefore_Complete()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
class C
{    
}";

        var immutableCompilation = testContext.CreateCompilationModel( code );
        Assert.Empty( immutableCompilation.Types.Single().Types );

        var compilation = immutableCompilation.CreateMutableClone();

        var type = Assert.Single( compilation.Types );

        // Add a nested type.
        var typeBuilder = new NamedTypeBuilder( null!, type, "T", TypeKind.Class );
        typeBuilder.Freeze();
        compilation.AddTransformation( typeBuilder.CreateTransformation() );

        // Assert that the type has been added.
        Assert.Single( type.Types );

        // Assert that there is still no type in original compilation.
        Assert.Empty( immutableCompilation.Types.Single().Types );
    }

    [Fact]
    public void AddNestedTypeToEmptyType_DontInitializeBefore_OfName()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
class C
{    
}";

        var immutableCompilation = testContext.CreateCompilationModel( code );
        Assert.Empty( immutableCompilation.Types.Single().Types.OfName( "T" ) );

        var compilation = immutableCompilation.CreateMutableClone();

        var type = Assert.Single( compilation.Types );

        // Add a nested type.
        var typeBuilder = new NamedTypeBuilder( null!, type, "T", TypeKind.Class );
        typeBuilder.Freeze();
        compilation.AddTransformation( typeBuilder.CreateTransformation() );

        // Assert that the type has been added.
        Assert.Single( type.Types.OfName( "T" ) );

        // Assert that there is still no type in original compilation.
        Assert.Empty( immutableCompilation.Types.Single().Types.OfName( "T" ) );
    }

    [Fact]
    public void AddNestedTypeToNonEmptyType_DontInitializeBefore_ByName()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
class C
{    
    class T<U>;
}";

        var immutableCompilation = testContext.CreateCompilationModel( code );
        var compilation = immutableCompilation.CreateMutableClone();

        var type = Assert.Single( compilation.Types );

        // Add a nested type.
        var typeBuilder = new NamedTypeBuilder( null!, type, "T", TypeKind.Class );
        typeBuilder.Freeze();
        compilation.AddTransformation( typeBuilder.CreateTransformation() );

        // Assert that the method has been added.
        Assert.Equal( 2, type.Types.OfName( "T" ).Count() );
    }

    [Fact]
    public void AddNestedTypeToNonEmptyType_InitializeBefore_ByName()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
class C
{    
    class T<U>;
}";

        var immutableCompilation = testContext.CreateCompilationModel( code );
        var compilation = immutableCompilation.CreateMutableClone();

        var type = Assert.Single( compilation.Types );

        // Instantiate the memoize the collection of nested types.
        Assert.Single( type.Types.OfName( "T" ) );

        // Add a nested type.
        var typeBuilder = new NamedTypeBuilder( null!, type, "T", TypeKind.Class );
        typeBuilder.Freeze();
        compilation.AddTransformation( typeBuilder.CreateTransformation() );

        // Assert that the method has been added.
        Assert.Equal( 2, type.Types.OfName( "T" ).Count() );
    }

    [Fact]
    public void AddNamespace_DontInitializeBefore_Complete()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
class C
{    
}";

        var immutableCompilation = testContext.CreateCompilationModel( code );
        Assert.Empty( immutableCompilation.GlobalNamespace.Namespaces );

        var compilation = immutableCompilation.CreateMutableClone();

        var globalNamespace = compilation.GlobalNamespace;

        // Add a namespace.
        var namespaceBuilder = new NamespaceBuilder( null!, globalNamespace, "N" );
        namespaceBuilder.Freeze();
        compilation.AddTransformation( namespaceBuilder.CreateTransformation() );

        // Assert that the type has been added.
        Assert.Single( globalNamespace.Namespaces );

        // Assert that there is still no namespace in original compilation.
        Assert.Empty( immutableCompilation.GlobalNamespace.Namespaces );
    }

    [Fact]
    public void AddNamespace_DontInitializeBefore_OfName()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
class C
{    
}";

        var immutableCompilation = testContext.CreateCompilationModel( code );
        Assert.Empty( immutableCompilation.GlobalNamespace.Namespaces );

        var compilation = immutableCompilation.CreateMutableClone();

        var globalNamespace = compilation.GlobalNamespace;

        // Add a namespace.
        var namespaceBuilder = new NamespaceBuilder( null!, globalNamespace, "N" );
        namespaceBuilder.Freeze();

        compilation.AddTransformation( namespaceBuilder.CreateTransformation() );

        // Assert that the type has been added.
        Assert.NotNull( globalNamespace.Namespaces.OfName( "N" ) );

        // Assert that there is still no type in original compilation.
        Assert.Null( immutableCompilation.GlobalNamespace.Namespaces.OfName( "N" ) );
    }

    [Fact]
    public void AddNamespace_InitializeBefore_Complete()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
class C
{    
    class T<U>;
}";

        var immutableCompilation = testContext.CreateCompilationModel( code );
        Assert.Empty( immutableCompilation.GlobalNamespace.Namespaces );

        var compilation = immutableCompilation.CreateMutableClone();

        var globalNamespace = compilation.GlobalNamespace;

        // Instantiate the memoize the collection of nested types.
        Assert.Empty( globalNamespace.Namespaces );

        // Add a namespace.
        var namespaceBuilder = new NamespaceBuilder( null!, globalNamespace, "N" );
        namespaceBuilder.Freeze();
        compilation.AddTransformation( namespaceBuilder.CreateTransformation() );

        // Assert that the type has been added.
        Assert.Single( globalNamespace.Namespaces );

        // Assert that there is still no namespace in original compilation.
        Assert.Empty( immutableCompilation.GlobalNamespace.Namespaces );
    }

    [Fact]
    public void AddNamespace_InitializeBefore_ByName()
    {
        using var testContext = this.CreateTestContext();

        const string code = "";

        var immutableCompilation = testContext.CreateCompilationModel( code );
        Assert.Empty( immutableCompilation.GlobalNamespace.Namespaces );

        var compilation = immutableCompilation.CreateMutableClone();

        var globalNamespace = compilation.GlobalNamespace;

        // Instantiate the memoize the collection of nested types.
        Assert.Null( globalNamespace.Namespaces.OfName( "N" ) );

        // Add a namespace.
        var namespaceBuilder = new NamespaceBuilder( null!, globalNamespace, "N" );
        namespaceBuilder.Freeze();
        compilation.AddTransformation( namespaceBuilder.CreateTransformation() );

        // Assert that the type has been added.
        Assert.NotNull( globalNamespace.Namespaces.OfName( "N" ) );

        // Assert that there is still no type in original compilation.
        Assert.Null( immutableCompilation.GlobalNamespace.Namespaces.OfName( "N" ) );
    }

    [Fact]
    public void AddMethodToGenericType_ParameterTypesMappedInDerivedType()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
class C<T>;
class D : C<int>;
";

        var immutableCompilation = testContext.CreateCompilationModel( code );
        var compilation = immutableCompilation.CreateMutableClone();

        var c = compilation.Types.OfName( "C" ).Single();

        // Add a method.
        var methodBuilder = new MethodBuilder( null!, c, "M" ) { ReturnType = c.TypeParameters[0] };
        methodBuilder.AddParameter( "p", c.TypeParameters[0] );
        methodBuilder.Freeze();

        compilation.AddTransformation( methodBuilder.ToTransformation() );

        // Get the method in the derived type.
        var methodInDerivedType = compilation.Types.OfName( "D" ).Single().AllMethods.OfName( "M" ).Single();

        // Assert that the type is as expected.
        Assert.Equal( SpecialType.Int32, methodInDerivedType.ReturnType.SpecialType );
        Assert.Equal( SpecialType.Int32, methodInDerivedType.Parameters[0].Type.SpecialType );
    }
}