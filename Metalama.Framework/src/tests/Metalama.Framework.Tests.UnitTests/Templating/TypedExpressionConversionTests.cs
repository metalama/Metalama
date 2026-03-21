// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Framework.Engine.Templating;
using Metalama.Framework.Engine.Templating.Expressions;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Templating;

public sealed class TypedExpressionConversionTests : UnitTestClass
{
    private const string _code = @"
class C { void M(int i, long l, object o, string s) {} }
class A {}
class B : A {}
";

    #region Group 1: TypedExpressionSyntaxImpl.Convert() — direct unit tests

    [Fact]
    public void Convert_NullExpressionType_AddsCast()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );
        var intType = compilation.Factory.GetTypeByReflectionType( typeof(int) );

        var syntaxSerializationContext = new SyntaxSerializationContext( compilation, SyntaxGenerationOptions.Formatted );

        using ( TemplateExpansionContext.WithTestingContext( syntaxSerializationContext, testContext.ServiceProvider ) )
        {
            // ExpressionType is null (bare identifier, no type annotation).
            var expr = new TypedExpressionSyntaxImpl(
                SyntaxFactory.ParseExpression( "x" ),
                compilation );

            var converted = expr.Convert( intType, syntaxSerializationContext.SyntaxGenerationContext, requireExactType: true );

            Assert.Equal( "((global::System.Int32)x)", converted.Syntax.NormalizeWhitespace().ToString() );
        }
    }

    [Fact]
    public void Convert_SameType_NoCast()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );
        var intType = compilation.Factory.GetTypeByReflectionType( typeof(int) );

        var syntaxSerializationContext = new SyntaxSerializationContext( compilation, SyntaxGenerationOptions.Formatted );

        using ( TemplateExpansionContext.WithTestingContext( syntaxSerializationContext, testContext.ServiceProvider ) )
        {
            var expr = new TypedExpressionSyntaxImpl(
                SyntaxFactory.ParseExpression( "42" ),
                intType,
                compilation );

            var converted = expr.Convert( intType, syntaxSerializationContext.SyntaxGenerationContext, requireExactType: true );

            Assert.Equal( "42", converted.Syntax.NormalizeWhitespace().ToString() );
        }
    }

    [Fact]
    public void Convert_DifferentType_ExactRequired_AddsCast()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );
        var objectType = compilation.Factory.GetTypeByReflectionType( typeof(object) );
        var intType = compilation.Factory.GetTypeByReflectionType( typeof(int) );

        var syntaxSerializationContext = new SyntaxSerializationContext( compilation, SyntaxGenerationOptions.Formatted );

        using ( TemplateExpansionContext.WithTestingContext( syntaxSerializationContext, testContext.ServiceProvider ) )
        {
            var expr = new TypedExpressionSyntaxImpl(
                SyntaxFactory.ParseExpression( "x" ),
                objectType,
                compilation );

            var converted = expr.Convert( intType, syntaxSerializationContext.SyntaxGenerationContext, requireExactType: true );

            Assert.Equal( "((global::System.Int32)x)", converted.Syntax.NormalizeWhitespace().ToString() );
        }
    }

    [Fact]
    public void Convert_ImplicitlyConvertible_ExactNotRequired_NoCast()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );
        var intType = compilation.Factory.GetTypeByReflectionType( typeof(int) );
        var longType = compilation.Factory.GetTypeByReflectionType( typeof(long) );

        var syntaxSerializationContext = new SyntaxSerializationContext( compilation, SyntaxGenerationOptions.Formatted );

        using ( TemplateExpansionContext.WithTestingContext( syntaxSerializationContext, testContext.ServiceProvider ) )
        {
            var expr = new TypedExpressionSyntaxImpl(
                SyntaxFactory.ParseExpression( "42" ),
                intType,
                compilation );

            var converted = expr.Convert( longType, syntaxSerializationContext.SyntaxGenerationContext, requireExactType: false );

            Assert.Equal( "42", converted.Syntax.NormalizeWhitespace().ToString() );
        }
    }

    [Fact]
    public void Convert_ImplicitlyConvertible_ExactRequired_AddsCast()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );
        var intType = compilation.Factory.GetTypeByReflectionType( typeof(int) );
        var longType = compilation.Factory.GetTypeByReflectionType( typeof(long) );

        var syntaxSerializationContext = new SyntaxSerializationContext( compilation, SyntaxGenerationOptions.Formatted );

        using ( TemplateExpansionContext.WithTestingContext( syntaxSerializationContext, testContext.ServiceProvider ) )
        {
            var expr = new TypedExpressionSyntaxImpl(
                SyntaxFactory.ParseExpression( "42" ),
                intType,
                compilation );

            var converted = expr.Convert( longType, syntaxSerializationContext.SyntaxGenerationContext, requireExactType: true );

            Assert.Equal( "((global::System.Int64)42)", converted.Syntax.NormalizeWhitespace().ToString() );
        }
    }

    [Fact]
    public void Convert_SubclassToBase_ExactRequired_AddsCast()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );
        var typeA = compilation.Types.OfName( "A" ).Single();
        var typeB = compilation.Types.OfName( "B" ).Single();

        var syntaxSerializationContext = new SyntaxSerializationContext( compilation, SyntaxGenerationOptions.Formatted );

        using ( TemplateExpansionContext.WithTestingContext( syntaxSerializationContext, testContext.ServiceProvider ) )
        {
            var expr = new TypedExpressionSyntaxImpl(
                SyntaxFactory.ParseExpression( "x" ),
                typeB,
                compilation );

            var converted = expr.Convert( typeA, syntaxSerializationContext.SyntaxGenerationContext, requireExactType: true );

            Assert.Equal( "((global::A)x)", converted.Syntax.NormalizeWhitespace().ToString() );
        }
    }

    [Fact]
    public void Convert_SubclassToBase_ExactNotRequired_NoCast()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );
        var typeA = compilation.Types.OfName( "A" ).Single();
        var typeB = compilation.Types.OfName( "B" ).Single();

        var syntaxSerializationContext = new SyntaxSerializationContext( compilation, SyntaxGenerationOptions.Formatted );

        using ( TemplateExpansionContext.WithTestingContext( syntaxSerializationContext, testContext.ServiceProvider ) )
        {
            var expr = new TypedExpressionSyntaxImpl(
                SyntaxFactory.ParseExpression( "x" ),
                typeB,
                compilation );

            var converted = expr.Convert( typeA, syntaxSerializationContext.SyntaxGenerationContext, requireExactType: false );

            Assert.Equal( "x", converted.Syntax.NormalizeWhitespace().ToString() );
        }
    }

    [Fact]
    public void Convert_NullLiteral_AlwaysCasts()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );
        var stringType = compilation.Factory.GetTypeByReflectionType( typeof(string) );
        var objectType = compilation.Factory.GetTypeByReflectionType( typeof(object) );

        var syntaxSerializationContext = new SyntaxSerializationContext( compilation, SyntaxGenerationOptions.Formatted );

        using ( TemplateExpansionContext.WithTestingContext( syntaxSerializationContext, testContext.ServiceProvider ) )
        {
            // ExpressionType=string, targetType=object. string is implicitly convertible to object,
            // so without the null literal special case, no cast would be needed.
            var expr = new TypedExpressionSyntaxImpl(
                SyntaxFactory.ParseExpression( "null" ),
                stringType,
                compilation );

            // Even with requireExactType=false and an implicit conversion, null literals must always be cast.
            var converted = expr.Convert( objectType, syntaxSerializationContext.SyntaxGenerationContext, requireExactType: false );

            Assert.Equal( "((global::System.Object)null)", converted.Syntax.NormalizeWhitespace().ToString() );
        }
    }

    #endregion

    #region Group 2: SyntaxUserExpression — type propagation and round-trips

    [Fact]
    public void SyntaxUserExpression_NoTargetType_PreservesType()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );
        var intType = compilation.Factory.GetTypeByReflectionType( typeof(int) );

        var syntaxSerializationContext = new SyntaxSerializationContext( compilation, SyntaxGenerationOptions.Formatted );

        using ( TemplateExpansionContext.WithTestingContext( syntaxSerializationContext, testContext.ServiceProvider ) )
        {
            var userExpr = new SyntaxUserExpression( SyntaxFactory.ParseExpression( "42" ), intType );

            var typed = userExpr.ToTypedExpressionSyntax( syntaxSerializationContext );

            Assert.Equal( "42", typed.Syntax.NormalizeWhitespace().ToString() );
            Assert.NotNull( typed.ExpressionType );
            Assert.True( compilation.Comparers.Default.Equals( intType, typed.ExpressionType ) );
        }
    }

    [Fact]
    public void SyntaxUserExpression_WithTargetType_PreservesOwnType()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );
        var objectType = compilation.Factory.GetTypeByReflectionType( typeof(object) );
        var intType = compilation.Factory.GetTypeByReflectionType( typeof(int) );

        var syntaxSerializationContext = new SyntaxSerializationContext( compilation, SyntaxGenerationOptions.Formatted );

        using ( TemplateExpansionContext.WithTestingContext( syntaxSerializationContext, testContext.ServiceProvider ) )
        {
            // Expression has type 'object'. SyntaxUserExpression ignores targetType, so syntax is always 'x'.
            var userExpr = new SyntaxUserExpression( SyntaxFactory.ParseExpression( "x" ), objectType );

            var typed = userExpr.ToTypedExpressionSyntax( syntaxSerializationContext, intType );

            // ExpressionType must be 'object' (this.Type), not 'int' (targetType).
            Assert.NotNull( typed.ExpressionType );
            Assert.True(
                compilation.Comparers.Default.Equals( objectType, typed.ExpressionType ),
                $"Expected ExpressionType to be 'object' but was '{typed.ExpressionType}'." );
        }
    }

    [Fact]
    public void SyntaxUserExpression_WithTargetType_RoundTripAddsCast()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );
        var objectType = compilation.Factory.GetTypeByReflectionType( typeof(object) );
        var intType = compilation.Factory.GetTypeByReflectionType( typeof(int) );

        var syntaxSerializationContext = new SyntaxSerializationContext( compilation, SyntaxGenerationOptions.Formatted );

        using ( TemplateExpansionContext.WithTestingContext( syntaxSerializationContext, testContext.ServiceProvider ) )
        {
            var userExpr = new SyntaxUserExpression( SyntaxFactory.ParseExpression( "x" ), objectType );
            var typed = userExpr.ToTypedExpressionSyntax( syntaxSerializationContext, intType );
            var converted = typed.Convert( intType, syntaxSerializationContext.SyntaxGenerationContext, requireExactType: true );

            // Should produce a cast because the expression type (object) differs from the target type (int).
            Assert.Equal( "((global::System.Int32)x)", converted.Syntax.NormalizeWhitespace().ToString() );
        }
    }

    [Fact]
    public void SyntaxUserExpression_SameType_RoundTripNoCast()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );
        var intType = compilation.Factory.GetTypeByReflectionType( typeof(int) );

        var syntaxSerializationContext = new SyntaxSerializationContext( compilation, SyntaxGenerationOptions.Formatted );

        using ( TemplateExpansionContext.WithTestingContext( syntaxSerializationContext, testContext.ServiceProvider ) )
        {
            var userExpr = new SyntaxUserExpression( SyntaxFactory.ParseExpression( "x" ), intType );
            var typed = userExpr.ToTypedExpressionSyntax( syntaxSerializationContext, intType );
            var converted = typed.Convert( intType, syntaxSerializationContext.SyntaxGenerationContext, requireExactType: true );

            Assert.Equal( "x", converted.Syntax.NormalizeWhitespace().ToString() );
        }
    }

    [Fact]
    public void SyntaxUserExpression_IntToLong_RoundTripAddsCast()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );
        var intType = compilation.Factory.GetTypeByReflectionType( typeof(int) );
        var longType = compilation.Factory.GetTypeByReflectionType( typeof(long) );

        var syntaxSerializationContext = new SyntaxSerializationContext( compilation, SyntaxGenerationOptions.Formatted );

        using ( TemplateExpansionContext.WithTestingContext( syntaxSerializationContext, testContext.ServiceProvider ) )
        {
            var userExpr = new SyntaxUserExpression( SyntaxFactory.ParseExpression( "0" ), intType );
            var typed = userExpr.ToTypedExpressionSyntax( syntaxSerializationContext, longType );
            var converted = typed.Convert( longType, syntaxSerializationContext.SyntaxGenerationContext, requireExactType: true );

            Assert.Equal( "((global::System.Int64)0)", converted.Syntax.NormalizeWhitespace().ToString() );
        }
    }

    #endregion

    #region Group 3: SourceUserExpression — casts to this.Type when targetType differs

    [Fact]
    public void SourceUserExpression_SameTargetType_NoSelfCast()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );
        var intType = compilation.Factory.GetTypeByReflectionType( typeof(int) );

        var syntaxSerializationContext = new SyntaxSerializationContext( compilation, SyntaxGenerationOptions.Formatted );

        using ( TemplateExpansionContext.WithTestingContext( syntaxSerializationContext, testContext.ServiceProvider ) )
        {
            // When targetType == this.Type, SourceUserExpression returns the expression unchanged.
            var sourceExpr = new SourceUserExpression( SyntaxFactory.ParseExpression( "x" ), intType );
            var typed = sourceExpr.ToTypedExpressionSyntax( syntaxSerializationContext, intType );

            // Syntax should be the original expression without a self-cast.
            Assert.Equal( "x", typed.Syntax.NormalizeWhitespace().ToString() );
            Assert.True( compilation.Comparers.Default.Equals( intType, typed.ExpressionType ) );
        }
    }

    [Fact]
    public void SourceUserExpression_DifferentTargetType_AddsSelfCast()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );
        var intType = compilation.Factory.GetTypeByReflectionType( typeof(int) );
        var longType = compilation.Factory.GetTypeByReflectionType( typeof(long) );

        var syntaxSerializationContext = new SyntaxSerializationContext( compilation, SyntaxGenerationOptions.Formatted );

        using ( TemplateExpansionContext.WithTestingContext( syntaxSerializationContext, testContext.ServiceProvider ) )
        {
            // When targetType != this.Type, SourceUserExpression adds a cast to this.Type (not to targetType).
            var sourceExpr = new SourceUserExpression( SyntaxFactory.ParseExpression( "x" ), intType );
            var typed = sourceExpr.ToTypedExpressionSyntax( syntaxSerializationContext, longType );

            // The cast is to this.Type (int), not to targetType (long).
            Assert.Equal( "(global::System.Int32)x", typed.Syntax.NormalizeWhitespace().ToString() );

            // ExpressionType must be this.Type (int), not targetType (long).
            Assert.True(
                compilation.Comparers.Default.Equals( intType, typed.ExpressionType ),
                $"Expected ExpressionType to be 'int' but was '{typed.ExpressionType}'." );
        }
    }

    [Fact]
    public void SourceUserExpression_NullTargetType_AddsSelfCast()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );
        var intType = compilation.Factory.GetTypeByReflectionType( typeof(int) );

        var syntaxSerializationContext = new SyntaxSerializationContext( compilation, SyntaxGenerationOptions.Formatted );

        using ( TemplateExpansionContext.WithTestingContext( syntaxSerializationContext, testContext.ServiceProvider ) )
        {
            // When targetType is null, SourceUserExpression adds a defensive cast to this.Type.
            var sourceExpr = new SourceUserExpression( SyntaxFactory.ParseExpression( "x" ), intType );
            var typed = sourceExpr.ToTypedExpressionSyntax( syntaxSerializationContext );

            Assert.Equal( "(global::System.Int32)x", typed.Syntax.NormalizeWhitespace().ToString() );
            Assert.True( compilation.Comparers.Default.Equals( intType, typed.ExpressionType ) );
        }
    }

    #endregion

    #region Group 4: TypedDefaultUserExpression

    [Fact]
    public void TypedDefaultUserExpression_ValueType_ProducesDefault()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );
        var intType = compilation.Factory.GetTypeByReflectionType( typeof(int) );

        var syntaxSerializationContext = new SyntaxSerializationContext( compilation, SyntaxGenerationOptions.Formatted );

        using ( TemplateExpansionContext.WithTestingContext( syntaxSerializationContext, testContext.ServiceProvider ) )
        {
            var defaultExpr = new TypedDefaultUserExpression( intType );
            var typed = defaultExpr.ToTypedExpressionSyntax( syntaxSerializationContext, intType );

            // For value types with matching targetType, should produce "default" (unqualified).
            Assert.Equal( "default", typed.Syntax.NormalizeWhitespace().ToString() );
            Assert.True( compilation.Comparers.Default.Equals( intType, typed.ExpressionType ) );
        }
    }

    [Fact]
    public void TypedDefaultUserExpression_ValueType_NullTarget_ProducesQualifiedDefault()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );
        var intType = compilation.Factory.GetTypeByReflectionType( typeof(int) );

        var syntaxSerializationContext = new SyntaxSerializationContext( compilation, SyntaxGenerationOptions.Formatted );

        using ( TemplateExpansionContext.WithTestingContext( syntaxSerializationContext, testContext.ServiceProvider ) )
        {
            var defaultExpr = new TypedDefaultUserExpression( intType );
            var typed = defaultExpr.ToTypedExpressionSyntax( syntaxSerializationContext );

            // Without target type, should produce qualified default (e.g. "default(global::System.Int32)").
            var syntax = typed.Syntax.NormalizeWhitespace().ToString();

            Assert.Contains( "default", syntax, System.StringComparison.Ordinal );
            Assert.Contains( "Int32", syntax, System.StringComparison.Ordinal );
            Assert.True( compilation.Comparers.Default.Equals( intType, typed.ExpressionType ) );
        }
    }

    [Fact]
    public void TypedDefaultUserExpression_ReferenceType_PreservesNullableType()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );
        var stringType = compilation.Factory.GetTypeByReflectionType( typeof(string) );

        // No serialization context needed — this only tests the constructor's nullable handling.
        var defaultExpr = new TypedDefaultUserExpression( stringType );

        Assert.True(
            defaultExpr.Type.IsNullable == true,
            $"Expected Type.IsNullable to be true but was '{defaultExpr.Type.IsNullable}'." );
    }

    #endregion

    #region Group 5: CastUserExpression — always produces syntax of this.Type

    [Fact]
    public void CastUserExpression_ProducesCastToOwnType()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );
        var intType = compilation.Factory.GetTypeByReflectionType( typeof(int) );

        var syntaxSerializationContext = new SyntaxSerializationContext( compilation, SyntaxGenerationOptions.Formatted );

        using ( TemplateExpansionContext.WithTestingContext( syntaxSerializationContext, testContext.ServiceProvider ) )
        {
            var castExpr = new CastUserExpression( intType, SyntaxFactory.ParseExpression( "x" ) );
            var typed = castExpr.ToTypedExpressionSyntax( syntaxSerializationContext );

            Assert.Equal( "((global::System.Int32)x)", typed.Syntax.NormalizeWhitespace().ToString() );
            Assert.True( compilation.Comparers.Default.Equals( intType, typed.ExpressionType ) );
        }
    }

    [Fact]
    public void CastUserExpression_WithDifferentTargetType_ExpressionTypeIsOwnType()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );
        var intType = compilation.Factory.GetTypeByReflectionType( typeof(int) );
        var longType = compilation.Factory.GetTypeByReflectionType( typeof(long) );

        var syntaxSerializationContext = new SyntaxSerializationContext( compilation, SyntaxGenerationOptions.Formatted );

        using ( TemplateExpansionContext.WithTestingContext( syntaxSerializationContext, testContext.ServiceProvider ) )
        {
            // CastUserExpression always casts to this.Type (int), regardless of targetType.
            var castExpr = new CastUserExpression( intType, SyntaxFactory.ParseExpression( "x" ) );
            var typed = castExpr.ToTypedExpressionSyntax( syntaxSerializationContext, longType );

            Assert.True(
                compilation.Comparers.Default.Equals( intType, typed.ExpressionType ),
                $"Expected ExpressionType to be 'int' but was '{typed.ExpressionType}'." );
        }
    }

    #endregion

    #region Group 6: ParameterExpression — ignores targetType

    [Fact]
    public void ParameterExpression_ProducesIdentifier()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );
        var method = compilation.Types.OfName( "C" ).Single().Methods.OfName( "M" ).Single();
        var parameter = method.Parameters[0]; // int i

        var syntaxSerializationContext = new SyntaxSerializationContext( compilation, SyntaxGenerationOptions.Formatted );

        using ( TemplateExpansionContext.WithTestingContext( syntaxSerializationContext, testContext.ServiceProvider ) )
        {
            var paramExpr = new ParameterExpression( parameter );
            var typed = paramExpr.ToTypedExpressionSyntax( syntaxSerializationContext );

            Assert.Equal( "i", typed.Syntax.NormalizeWhitespace().ToString() );
            Assert.True( compilation.Comparers.Default.Equals( parameter.Type, typed.ExpressionType ) );
        }
    }

    [Fact]
    public void ParameterExpression_WithTargetType_PreservesOwnType()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );
        var method = compilation.Types.OfName( "C" ).Single().Methods.OfName( "M" ).Single();
        var parameter = method.Parameters[0]; // int i
        var longType = compilation.Factory.GetTypeByReflectionType( typeof(long) );

        var syntaxSerializationContext = new SyntaxSerializationContext( compilation, SyntaxGenerationOptions.Formatted );

        using ( TemplateExpansionContext.WithTestingContext( syntaxSerializationContext, testContext.ServiceProvider ) )
        {
            var paramExpr = new ParameterExpression( parameter );
            var typed = paramExpr.ToTypedExpressionSyntax( syntaxSerializationContext, longType );

            // ExpressionType should be int (parameter type), not long (target type).
            Assert.True(
                compilation.Comparers.Default.Equals( parameter.Type, typed.ExpressionType ),
                $"Expected ExpressionType to be 'int' but was '{typed.ExpressionType}'." );
        }
    }

    #endregion

    #region Group 7: TypeOfUserExpression — ignores targetType

    [Fact]
    public void TypeOfUserExpression_ProducesTypeOf()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );
        var intType = compilation.Factory.GetTypeByReflectionType( typeof(int) );
        var typeType = compilation.Factory.GetTypeByReflectionType( typeof(System.Type) );

        var syntaxSerializationContext = new SyntaxSerializationContext( compilation, SyntaxGenerationOptions.Formatted );

        using ( TemplateExpansionContext.WithTestingContext( syntaxSerializationContext, testContext.ServiceProvider ) )
        {
            var typeOfExpr = new TypeOfUserExpression( intType );
            var typed = typeOfExpr.ToTypedExpressionSyntax( syntaxSerializationContext );

            Assert.Contains( "typeof", typed.Syntax.NormalizeWhitespace().ToString(), System.StringComparison.Ordinal );
            Assert.True( compilation.Comparers.Default.Equals( typeType, typed.ExpressionType ) );
        }
    }

    #endregion

    #region Group 8: Full round-trip — various UserExpression subclasses → Convert()

    [Fact]
    public void SourceUserExpression_RoundTrip_DifferentTarget_AddsCast()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );
        var intType = compilation.Factory.GetTypeByReflectionType( typeof(int) );
        var longType = compilation.Factory.GetTypeByReflectionType( typeof(long) );

        var syntaxSerializationContext = new SyntaxSerializationContext( compilation, SyntaxGenerationOptions.Formatted );

        using ( TemplateExpansionContext.WithTestingContext( syntaxSerializationContext, testContext.ServiceProvider ) )
        {
            // SourceUserExpression with type int, targetType long.
            // ToSyntax produces a cast to int (self-cast): (global::System.Int32)x. ExpressionType is int.
            // Convert(long, requireExactType=true) should add a further cast to long.
            var sourceExpr = new SourceUserExpression( SyntaxFactory.ParseExpression( "x" ), intType );
            var typed = sourceExpr.ToTypedExpressionSyntax( syntaxSerializationContext, longType );
            var converted = typed.Convert( longType, syntaxSerializationContext.SyntaxGenerationContext, requireExactType: true );

            Assert.Equal( "((global::System.Int64)(global::System.Int32)x)", converted.Syntax.NormalizeWhitespace().ToString() );
        }
    }

    [Fact]
    public void ParameterExpression_RoundTrip_DifferentTarget_AddsCast()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );
        var method = compilation.Types.OfName( "C" ).Single().Methods.OfName( "M" ).Single();
        var parameter = method.Parameters[0]; // int i
        var longType = compilation.Factory.GetTypeByReflectionType( typeof(long) );

        var syntaxSerializationContext = new SyntaxSerializationContext( compilation, SyntaxGenerationOptions.Formatted );

        using ( TemplateExpansionContext.WithTestingContext( syntaxSerializationContext, testContext.ServiceProvider ) )
        {
            var paramExpr = new ParameterExpression( parameter );
            var typed = paramExpr.ToTypedExpressionSyntax( syntaxSerializationContext, longType );
            var converted = typed.Convert( longType, syntaxSerializationContext.SyntaxGenerationContext, requireExactType: true );

            // Should cast identifier 'i' (int) to long.
            Assert.Equal( "((global::System.Int64)i)", converted.Syntax.NormalizeWhitespace().ToString() );
        }
    }

    #endregion

    #region Group 9: TypedExpressionSyntaxImpl.ToUserExpression round-trip preservation

    [Fact]
    public void ToUserExpression_PreservesOriginatingExpression()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );
        var stringType = compilation.Factory.GetTypeByReflectionType( typeof(string) );

        var syntaxSerializationContext = new SyntaxSerializationContext( compilation, SyntaxGenerationOptions.Formatted );

        using ( TemplateExpansionContext.WithTestingContext( syntaxSerializationContext, testContext.ServiceProvider ) )
        {
            var originalExpr = new SyntaxUserExpression( SyntaxFactory.ParseExpression( @"""hello""" ), stringType );

            // Wrap in TypedExpressionSyntaxImpl via ToTypedExpressionSyntax (which sets originating expression).
            var typed = originalExpr.ToTypedExpressionSyntax( syntaxSerializationContext );

            // Round-trip back.
            var roundTripped = typed.ToUserExpression( compilation );

            // Should return the same SyntaxUserExpression instance.
            Assert.Same( originalExpr, roundTripped );
        }
    }

    [Fact]
    public void ToUserExpression_NullExpressionType_FallsBackToObject()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );
        var objectType = compilation.Factory.GetTypeByReflectionType( typeof(object) );

        var syntaxSerializationContext = new SyntaxSerializationContext( compilation, SyntaxGenerationOptions.Formatted );

        using ( TemplateExpansionContext.WithTestingContext( syntaxSerializationContext, testContext.ServiceProvider ) )
        {
            // Create with null ExpressionType (bare identifier, no annotation).
            var typed = new TypedExpressionSyntaxImpl(
                SyntaxFactory.ParseExpression( "x" ),
                compilation );

            var userExpr = typed.ToUserExpression( compilation );

            // When ExpressionType is null, ToUserExpression should fall back to object.
            Assert.True(
                compilation.Comparers.Default.Equals( objectType, userExpr.Type ),
                $"Expected type 'object' but was '{userExpr.Type}'." );
        }
    }

    [Fact]
    public void ToUserExpression_WithType_PreservesType()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );
        var stringType = compilation.Factory.GetTypeByReflectionType( typeof(string) );

        var syntaxSerializationContext = new SyntaxSerializationContext( compilation, SyntaxGenerationOptions.Formatted );

        using ( TemplateExpansionContext.WithTestingContext( syntaxSerializationContext, testContext.ServiceProvider ) )
        {
            var typed = new TypedExpressionSyntaxImpl(
                SyntaxFactory.ParseExpression( @"""hello""" ),
                stringType,
                compilation );

            var userExpr = typed.ToUserExpression( compilation );

            Assert.True(
                compilation.Comparers.Default.Equals( stringType, userExpr.Type ),
                $"Expected type 'string' but was '{userExpr.Type}'." );
        }
    }

    #endregion
}
