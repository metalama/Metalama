// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Comparers;
using Metalama.Framework.Engine.AdviceImpl.Introduction;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.Introductions.Introduced;
using Metalama.Framework.Engine.CodeModel.Source;
using Metalama.Testing.UnitTesting;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CodeModel;

public sealed class PromotedFieldTests : UnitTestClass
{
    [Fact]
    public void NonGenericTest()
    {
        using var testContext = this.CreateTestContext();

        // Create original compilation.
        const string code = @"
class C
{    
 int _f;
}";

        var immutableCompilation = testContext.CreateCompilationModel( code );
        var field = immutableCompilation.Types.Single().Fields.Single();

        // Create a PromotedField.
        var promoteFieldTransformation = PromoteFieldTransformation.Create( testContext.ServiceProvider, field, null! );
        var overridingProperty = promoteFieldTransformation.OverridingProperty;
        Assert.Same( overridingProperty.Definition, overridingProperty );
        Assert.Same( overridingProperty.OriginalField, field );

        // Verify that all properties work.
        CheckDeclarationProperties( testContext, overridingProperty );

        // Add the PromotedField to a compilation.
        var compilation = immutableCompilation.CreateMutableClone();
        compilation.AddTransformation( promoteFieldTransformation );

        // Assertions on declarations.
        var fieldAfter = field.ForCompilation( compilation );
        Assert.IsType<SourceField>( fieldAfter );
        Assert.NotNull( fieldAfter.OverridingProperty );
        Assert.NotNull( fieldAfter.OverridingProperty.OriginalField );
        Assert.Same( fieldAfter, fieldAfter.OverridingProperty.OriginalField );
        Assert.Equal( DeclarationKind.Field, fieldAfter.DeclarationKind );
        Assert.Equal( DeclarationKind.Property, fieldAfter.OverridingProperty.DeclarationKind );
        Assert.Same( fieldAfter, fieldAfter.GetMethod.ContainingDeclaration );
        Assert.Same( fieldAfter, fieldAfter.SetMethod.ContainingDeclaration );
        Assert.Same( fieldAfter.OverridingProperty, fieldAfter.OverridingProperty.GetMethod.ContainingDeclaration );
        Assert.Same( fieldAfter.OverridingProperty, fieldAfter.OverridingProperty.SetMethod.ContainingDeclaration );
        Assert.Same( fieldAfter.OverridingProperty, fieldAfter.OverridingProperty.SetMethod.ReturnParameter.ContainingDeclaration.ContainingDeclaration );

        // Declaration on references.
        Assert.Same( fieldAfter, field.ToRef().GetTarget( compilation ) );
        Assert.True( RefEqualityComparer<IField>.Default.Equals( field.ToRef(), overridingProperty.ToRef().As<IField>() ) );
        Assert.NotEqual<IRef>( overridingProperty.ToRef(), overridingProperty.ToRef().As<IField>() );
    }

    [Fact]
    public void GenericTest()
    {
        using var testContext = this.CreateTestContext();

        // Create original compilation.
        const string code = @"
class C<T>
{    
 int _f;
}";

        var immutableCompilation = testContext.CreateCompilationModel( code );

        using ( testContext.WithExecutionContext( immutableCompilation ) )
        {
            var field = immutableCompilation.Types.Single().Fields.Single();

            // Create a PromotedField.
            var promoteFieldTransformation = PromoteFieldTransformation.Create( testContext.ServiceProvider, field, null! );

            // Add the PromotedField to a compilation.
            var compilation = immutableCompilation.CreateMutableClone();
            compilation.AddTransformation( promoteFieldTransformation );
            var builtPromotedField = field.ForCompilation( compilation );
            Assert.Same( builtPromotedField.Definition, builtPromotedField );
            Assert.Same( builtPromotedField.OverridingProperty.OriginalField, builtPromotedField );

            // Get generic instances.
            var genericField = field.ForTypeInstance( field.DeclaringType.MakeGenericInstance( typeof(int) ) );
            var genericPromotedField = builtPromotedField.ForTypeInstance( builtPromotedField.DeclaringType.MakeGenericInstance( typeof(int) ) );

            // Assertions on declarations.
            var genericFieldAfter = genericField.ForCompilation( compilation );
            Assert.IsType<SourceField>( genericFieldAfter );
            Assert.Equal( SpecialType.Int32, genericFieldAfter.Type.SpecialType );
            Assert.NotNull( genericFieldAfter.OverridingProperty );
            Assert.Equal( SpecialType.Int32, genericFieldAfter.OverridingProperty.Type.SpecialType );
            Assert.NotNull( genericFieldAfter.OverridingProperty.OriginalField );
            Assert.Same( genericFieldAfter, genericFieldAfter.OverridingProperty.OriginalField );
            Assert.Equal( DeclarationKind.Field, genericFieldAfter.DeclarationKind );
            Assert.Equal( DeclarationKind.Property, genericFieldAfter.OverridingProperty.DeclarationKind );
            Assert.Same( genericFieldAfter, genericFieldAfter.GetMethod.ContainingDeclaration );
            Assert.Same( genericFieldAfter, genericFieldAfter.SetMethod.ContainingDeclaration );
            Assert.Same( genericFieldAfter.OverridingProperty, genericFieldAfter.OverridingProperty.GetMethod.ContainingDeclaration );
            Assert.Same( genericFieldAfter.OverridingProperty, genericFieldAfter.OverridingProperty.SetMethod.ContainingDeclaration );

            // Declaration on references.
            Assert.Same( genericFieldAfter, genericField.ToRef().GetTarget( compilation ) );
            Assert.True( RefEqualityComparer<IField>.Default.Equals( genericField.ToRef(), genericPromotedField.ToRef().As<IField>() ) );
        }
    }

    [Fact]
    public void Introduced()
    {
        using var testContext = this.CreateTestContext();

        // Create original compilation.
        const string code = @"
class C
{
}";

        var immutableCompilation1 = testContext.CreateCompilationModel( code );

        var introducedField = new FieldBuilder( null!, immutableCompilation1.Types.Single(), "_f" );
        introducedField.Accessibility = Accessibility.Private;
        introducedField.Type = immutableCompilation1.Factory.GetSpecialType( SpecialType.Int32 );
        introducedField.Freeze();

        // Add an introduced field to a compilation.
        var compilation1 = immutableCompilation1.CreateMutableClone();
        compilation1.AddTransformation( introducedField.CreateTransformation() );

        var immutableCompilation2 = compilation1.CreateImmutableClone();

        var field = immutableCompilation2.Types.Single().Fields.Single();

        // Create a PromotedField.
        var promoteFieldTransformation = PromoteFieldTransformation.Create( testContext.ServiceProvider, field, null! );
        var overridingProperty = promoteFieldTransformation.OverridingProperty;
        Assert.Same( overridingProperty.Definition, overridingProperty );
        Assert.Same( overridingProperty.OriginalField, field );

        _ = overridingProperty.PrimarySyntaxTree;

        // Verify that all properties work.
        CheckDeclarationProperties( testContext, overridingProperty );

        // Add the PromotedField to a compilation.
        var compilation2 = immutableCompilation1.CreateMutableClone();
        compilation2.AddTransformation( promoteFieldTransformation );

        // Assertions on declarations.
        var fieldAfter = field.ForCompilation( compilation2 );
        Assert.IsType<IntroducedField>( fieldAfter );
        Assert.NotNull( fieldAfter.OverridingProperty );
        Assert.NotNull( fieldAfter.OverridingProperty.OriginalField );
        Assert.Same( fieldAfter, fieldAfter.OverridingProperty.OriginalField );
        Assert.Equal( DeclarationKind.Field, fieldAfter.DeclarationKind );
        Assert.Equal( DeclarationKind.Property, fieldAfter.OverridingProperty.DeclarationKind );
        Assert.Same( fieldAfter, fieldAfter.GetMethod.ContainingDeclaration );
        Assert.Same( fieldAfter, fieldAfter.SetMethod.ContainingDeclaration );
        Assert.Same( fieldAfter.OverridingProperty, fieldAfter.OverridingProperty.GetMethod.ContainingDeclaration );
        Assert.Same( fieldAfter.OverridingProperty, fieldAfter.OverridingProperty.SetMethod.ContainingDeclaration );

        // Declaration on references.
        Assert.Same( fieldAfter, field.ToRef().GetTarget( compilation2 ) );
        Assert.True( RefEqualityComparer<IField>.Default.Equals( field.ToRef(), overridingProperty.ToRef().As<IField>() ) );
        Assert.NotEqual<IRef>( overridingProperty.ToRef(), overridingProperty.ToRef().As<IField>() );
    }

    private static void CheckDeclarationProperties( TestContext testContext, IDeclaration declaration )
    {
        var objectReader = new ObjectReaderFactory().GetReader( testContext.ServiceProvider, declaration );

        foreach ( var property in objectReader.Keys )
        {
            if ( property != nameof(IDeclaration.Origin) )
            {
                _ = objectReader[property];
            }
        }
    }
}