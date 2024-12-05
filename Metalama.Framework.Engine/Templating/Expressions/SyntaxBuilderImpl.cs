// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Invokers;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Framework.Engine.Templating.Statements;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Project;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using System;
using System.Collections.Immutable;
using System.Text;
using SpecialType = Metalama.Framework.Code.SpecialType;
using TypedConstant = Metalama.Framework.Code.TypedConstant;

namespace Metalama.Framework.Engine.Templating.Expressions;

internal class SyntaxBuilderImpl : ISyntaxBuilderImpl
{
    // Note that the implementation of this class cannot use TemplateExpansionContext because there is no necessarily one active.
    // For instance, in the BuildAspect method, there is none.

    private readonly SyntaxGenerationContext _syntaxGenerationContext;
    private readonly INamedType? _currentType;
    private readonly IType _targetTypedExpressionType;

    ICompilation ISyntaxBuilderImpl.Compilation => this.Compilation;

    public CompilationModel Compilation { get; }

    private ContextualSyntaxGenerator SyntaxGenerator => this._syntaxGenerationContext.SyntaxGenerator;

    protected SyntaxBuilderImpl( CompilationModel compilation, SyntaxGenerationContext syntaxGenerationContext, INamedType? currentType )
    {
        this.Compilation = compilation;
        this._syntaxGenerationContext = syntaxGenerationContext;
        this._currentType = currentType;

        // The IExpression interface does not allow to represent target-typed expressions (such as `null` or `default`), so we use `object` instead.
        this._targetTypedExpressionType = compilation.Factory.GetSpecialType( SpecialType.Object );
    }

    public SyntaxBuilderImpl( CompilationModel compilation, SyntaxGenerationOptions syntaxGenerationOptions, INamedType? currentType )
        : this( compilation, compilation.CompilationContext.GetSyntaxGenerationContext( syntaxGenerationOptions ), currentType ) { }

    public IProject Project => this.Compilation.Project;

    public IExpression Capture( object? expression ) => new CapturedUserExpression( this.Compilation, expression );

    public IExpression BuildArray( ArrayBuilder arrayBuilder ) => new ArrayUserExpression( arrayBuilder );

    public IExpression BuildInterpolatedString( InterpolatedStringBuilder interpolatedStringBuilder )
        => new InterpolatedStringUserExpression( interpolatedStringBuilder, this.Compilation );

    public IExpression ParseExpression( string code, IType? type, bool? isReferenceable )
    {
        var expression = SyntaxFactoryEx.ParseExpressionSafe( code )
            .WithAdditionalAnnotations( Formatter.Annotation )
            .WithSimplifierAnnotation();

        return new SyntaxUserExpression( expression, type ?? this.Compilation.Cache.SystemObjectType, isReferenceable );
    }

    public IStatement ParseStatement( string code )
    {
        var statement = SyntaxFactoryEx.ParseStatementSafe( code );

        return new UserStatement( statement );
    }

    public IStatement CreateExpressionStatement( IExpression expression )
        => new UserStatement(
            SyntaxFactory.ExpressionStatement( ((UserExpression) expression).ToExpressionSyntax( this.CreateSyntaxSerializationContext() ) ) );

    private SyntaxSerializationContext CreateSyntaxSerializationContext() => new( this.Compilation, this._syntaxGenerationContext, this._currentType );

    public void AppendLiteral( object? value, StringBuilder stringBuilder, SpecialType specialType, bool stronglyTyped )
    {
        if ( value == null )
        {
            stringBuilder.Append( stronglyTyped ? "default(string)" : "null" );
        }
        else
        {
            var expression = this.GetLiteralImpl( value, specialType, stronglyTyped );
            stringBuilder.Append( expression.ToFullString() );
        }
    }

    private ExpressionSyntax GetLiteralImpl( object value, SpecialType specialType, bool stronglyTyped )
    {
        var options = stronglyTyped ? ObjectDisplayOptions.IncludeTypeSuffix : ObjectDisplayOptions.None;
        var expression = SyntaxFactoryEx.LiteralExpression( value, options );

        if ( stronglyTyped && specialType != SpecialType.String )
        {
            var cast = specialType switch
            {
                SpecialType.Byte => SyntaxKind.ByteKeyword,
                SpecialType.SByte => SyntaxKind.SByteKeyword,
                SpecialType.Int16 => SyntaxKind.ShortKeyword,
                SpecialType.UInt16 => SyntaxKind.UShortKeyword,
                _ => SyntaxKind.None
            };

            if ( cast != SyntaxKind.None )
            {
                return this._syntaxGenerationContext.SyntaxGenerator.SafeCastExpression(
                    SyntaxFactory.PredefinedType( SyntaxFactory.Token( cast ) ),
                    expression );
            }
        }

        return expression;
    }

    public IExpression Literal( object? value, SpecialType specialType, bool stronglyTyped )
    {
        ExpressionSyntax expression;

        if ( specialType == SpecialType.None )
        {
            // If we didn't get the specialType, resolve it dynamically.
            specialType = value switch
            {
                null => SpecialType.String,
                byte => SpecialType.Byte,
                sbyte => SpecialType.SByte,
                string => SpecialType.String,
                char => SpecialType.Char,
                int => SpecialType.Int32,
                uint => SpecialType.UInt32,
                long => SpecialType.Int64,
                ulong => SpecialType.UInt64,
                short => SpecialType.Int16,
                ushort => SpecialType.UInt16,
                double => SpecialType.Double,
                float => SpecialType.Single,
                decimal => SpecialType.Decimal,
                bool => SpecialType.Boolean,
                Type => SpecialType.Type,
                _ => throw new ArgumentOutOfRangeException( nameof(value), $"{value.GetType().Name} is not a literal type." )
            };
        }

        INamedType type;

        if ( value == null )
        {
            expression = stronglyTyped
                ? SyntaxFactory.DefaultExpression( SyntaxFactory.PredefinedType( SyntaxFactory.Token( SyntaxKind.StringKeyword ) ) )
                : SyntaxFactoryEx.Null;

            type = this.Compilation.Factory.GetSpecialType( SpecialType.String ).ToNullable();
        }
        else
        {
            expression = this.GetLiteralImpl( value, specialType, stronglyTyped );
            type = this.Compilation.Factory.GetSpecialType( specialType );
        }

        return new SyntaxUserExpression( expression, type );
    }

    public void AppendTypeName( IType type, StringBuilder stringBuilder )
    {
        var code = this.SyntaxGenerator.TypeSyntax( type ).ToString();
        stringBuilder.Append( code );
    }

    public void AppendTypeName( Type type, StringBuilder stringBuilder )
        => this.AppendTypeName(
            this.Compilation.Factory.GetTypeByReflectionType( type ),
            stringBuilder );

    public void AppendExpression( IExpression expression, StringBuilder stringBuilder )
        => stringBuilder.Append(
            expression.ToExpressionSyntax( this.CreateSyntaxSerializationContext() )
                .NormalizeWhitespace()
                .ToFullString() );

    public void AppendDynamic( object? expression, StringBuilder stringBuilder )
        => stringBuilder.Append(
            expression == null
                ? "null"
                : TypedExpressionSyntaxImpl.GetSyntaxFromValue( expression, this.CreateSyntaxSerializationContext() )
                    .NormalizeWhitespace()
                    .ToFullString() );

    public IExpression Cast( IExpression expression, IType targetType )
        => expression.Type.IsConvertibleTo( targetType ) ? expression : new CastUserExpression( targetType, expression );

    public object TypedConstant( in TypedConstant typedConstant )
        => new SyntaxUserExpression( this.SyntaxGenerator.TypedConstant( typedConstant ), typedConstant.Type );

    public IExpression ThisExpression( INamedType type ) => new SyntaxUserExpression( SyntaxFactory.ThisExpression(), type );

    public IExpression ToExpression( IFieldOrProperty fieldOrProperty, IExpression? instance )
    {
        if ( fieldOrProperty is { DeclarationKind: DeclarationKind.Field, IsImplicitlyDeclared: true } )
        {
            throw new InvalidOperationException(
                MetalamaStringFormatter.Format( $"Cannot convert '{fieldOrProperty}' to an IExpression because it is an implicitly declared field." ) );
        }

        return new FieldOrPropertyInvoker( fieldOrProperty, InvokerOptions.Default, instance );
    }

    public IExpression ToExpression( IParameter parameter ) => new ParameterExpression( parameter );

    public IExpression WithType( IExpression expression, IType type ) => new OverrideTypeUserExpression( expression, type );

    public IStatement CreateTemplateStatement( TemplateInvocation templateInvocation, object? args )
        => new TemplateInvocationStatement( templateInvocation, args );

    public IStatement CreateSwitchStatement( IExpression expression, ImmutableArray<SwitchStatementSection> cases ) => new SwitchStatement( expression, cases );

    public IStatement CreateBlock( IStatementList statements ) => new BlockStatement( statements );

    public IExpression NullExpression( IType? type ) => new SyntaxUserExpression( SyntaxFactoryEx.Null, type?.ToNullable() ?? this._targetTypedExpressionType );

    public IExpression DefaultExpression( IType? type )
    {
        if ( type != null )
        {
            return new TypedDefaultUserExpression( type );
        }
        else
        {
            return new SyntaxUserExpression( SyntaxFactoryEx.Default, this._targetTypedExpressionType );
        }
    }

    public IStatementList UnwrapBlock( IStatement statement ) => new UnwrappedBlockStatementList( statement );

    public IStatementList CreateStatementList( ImmutableArray<object> items ) => new StatementList( items );
}