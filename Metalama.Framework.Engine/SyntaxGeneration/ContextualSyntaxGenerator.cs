// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Comparers;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using static Metalama.Framework.Engine.SyntaxGeneration.SyntaxFactoryEx;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SpecialType = Microsoft.CodeAnalysis.SpecialType;
using TypedConstant = Metalama.Framework.Code.TypedConstant;
using TypeKind = Metalama.Framework.Code.TypeKind;
using VarianceKind = Metalama.Framework.Code.VarianceKind;
using WorkspaceHelper = Metalama.Framework.Engine.Utilities.Roslyn.WorkspaceHelper;

namespace Metalama.Framework.Engine.SyntaxGeneration;

#pragma warning disable CA1822

internal sealed partial class ContextualSyntaxGenerator
{
    private static readonly SyntaxGenerator _roslynSyntaxGenerator;

    static ContextualSyntaxGenerator()
    {
        var type = WorkspaceHelper.CSharpWorkspacesAssembly.GetType( "Microsoft.CodeAnalysis.CSharp.CodeGeneration.CSharpSyntaxGenerator" )!;
        var field = type.GetField( "Instance", BindingFlags.Public | BindingFlags.Static )!;
        _roslynSyntaxGenerator = (SyntaxGenerator) field.GetValue( null ).AssertNotNull();
    }

    private readonly SyntaxGeneratorForIType _syntaxGeneratorForIType;
    private readonly ConcurrentDictionary<IRef<IType>, TypeSyntax> _typeSyntaxCache;
    private readonly ConcurrentDictionary<ITypeSymbol, TypeSyntax> _typeSymbolSyntaxCache;
    private readonly ConcurrentDictionary<IRef<IType>, ExpressionSyntax> _typeExpressionCache;
    private readonly ConcurrentDictionary<ITypeSymbol, ExpressionSyntax> _typeSymbolExpressionCache;

    public bool IsNullAware { get; }

    public SyntaxGenerationContext SyntaxGenerationContext { get; }

    internal ContextualSyntaxGenerator( SyntaxGenerationContext context, bool nullAware )
    {
        this.SyntaxGenerationContext = context;
        this._syntaxGeneratorForIType = new SyntaxGeneratorForIType( context.Options );
        this._typeSyntaxCache = new ConcurrentDictionary<IRef<IType>, TypeSyntax>( RefEqualityComparer<IType>.IncludeNullability );
        this._typeSymbolSyntaxCache = new ConcurrentDictionary<ITypeSymbol, TypeSyntax>( SymbolEqualityComparer.IncludeNullability );
        this._typeExpressionCache = new ConcurrentDictionary<IRef<IType>, ExpressionSyntax>( RefEqualityComparer<IType>.IncludeNullability );
        this._typeSymbolExpressionCache = new ConcurrentDictionary<ITypeSymbol, ExpressionSyntax>( SymbolEqualityComparer.IncludeNullability );
        this.IsNullAware = nullAware;
    }

    public TypeOfExpressionSyntax TypeOfExpression(
        ITypeSymbol type,
        IReadOnlyDictionary<string, TypeSyntax>? substitutions = null,
        bool keepNullableAnnotations = false )
    {
        var typeSyntax = this.TypeSyntax( type );

        if ( type is INamedTypeSymbol { IsGenericType: true } namedType )
        {
            if ( namedType.IsGenericTypeDefinition() )
            {
                // In generic definitions, we must remove type arguments.
                typeSyntax = (TypeSyntax) new RemoveTypeArgumentsRewriter().Visit( typeSyntax ).AssertNotNull();
            }
        }

        if ( !keepNullableAnnotations )
        {
            // In regular typeof, we must remove ? annotations of nullable types.
            typeSyntax = (TypeSyntax) new RemoveReferenceNullableAnnotationsRewriterForSymbol( type ).Visit( typeSyntax )!;
        }

        var dynamicToVarRewriter = new DynamicToVarRewriter();

        // In any typeof, we must change dynamic to object.
        typeSyntax = (TypeSyntax) dynamicToVarRewriter.Visit( typeSyntax ).AssertNotNull();

        SafeSyntaxRewriter rewriter = type switch
        {
            INamedTypeSymbol { IsGenericType: true } genericType when genericType.IsGenericTypeDefinition() => new RemoveTypeArgumentsRewriter(),
            INamedTypeSymbol { IsGenericType: true } => new RemoveReferenceNullableAnnotationsRewriterForSymbol( type ),
            _ => dynamicToVarRewriter
        };

        var rewrittenTypeSyntax = rewriter.Visit( typeSyntax ).AssertNotNull();

        // Substitute type arguments.
        if ( substitutions is { Count: > 0 } )
        {
            var substitutionRewriter = new SubstitutionRewriter( substitutions );
            rewrittenTypeSyntax = substitutionRewriter.Visit( rewrittenTypeSyntax ).AssertNotNull();
        }

        return (TypeOfExpressionSyntax) _roslynSyntaxGenerator.TypeOfExpression( rewrittenTypeSyntax );
    }

    public TypeOfExpressionSyntax TypeOfExpression(
        IType type,
        IReadOnlyDictionary<string, TypeSyntax>? substitutions = null,
        bool keepNullableAnnotations = false,
        bool bypassSymbols = false )
    {
        if ( type.GetSymbol() is { } symbol && !bypassSymbols )
        {
            return this.TypeOfExpression( symbol, substitutions, keepNullableAnnotations );
        }

        var typeSyntax = this.TypeSyntax( type );

        if ( type is INamedType { TypeParameters.Count: > 0, IsCanonicalGenericInstance: true } )
        {
            // In generic definitions, we must remove type arguments.
            typeSyntax = (TypeSyntax) new RemoveTypeArgumentsRewriter().Visit( typeSyntax ).AssertNotNull();
        }

        if ( !keepNullableAnnotations )
        {
            // In regular typeof, we must remove ? annotations of nullable types.
            typeSyntax = (TypeSyntax) new RemoveReferenceNullableAnnotationsRewriter( type ).Visit( typeSyntax )!;
        }

        var dynamicToVarRewriter = new DynamicToVarRewriter();

        // In any typeof, we must change dynamic to object.
        typeSyntax = (TypeSyntax) dynamicToVarRewriter.Visit( typeSyntax ).AssertNotNull();

        SafeSyntaxRewriter rewriter = type switch
        {
            INamedType { TypeParameters.Count: > 0, IsCanonicalGenericInstance: true } => new RemoveTypeArgumentsRewriter(),
            INamedType { TypeParameters.Count: > 0 } => new RemoveReferenceNullableAnnotationsRewriter( type ),
            _ => dynamicToVarRewriter
        };

        var rewrittenTypeSyntax = rewriter.Visit( typeSyntax ).AssertNotNull();

        // Substitute type arguments.
        if ( substitutions is { Count: > 0 } )
        {
            var substitutionRewriter = new SubstitutionRewriter( substitutions );
            rewrittenTypeSyntax = substitutionRewriter.Visit( rewrittenTypeSyntax ).AssertNotNull();
        }

        return (TypeOfExpressionSyntax) _roslynSyntaxGenerator.TypeOfExpression( rewrittenTypeSyntax );
    }

    public ExpressionSyntax DefaultExpression( IType type, IType? targetType = null )
    {
        if ( targetType == null )
        {
            return SyntaxFactory.DefaultExpression( this.TypeSyntax( type ) )
                .WithSimplifierAnnotationIfNecessary( this.SyntaxGenerationContext );
        }
        else if ( type.IsReferenceType == true )
        {
            return Null;
        }
        else
        {
            return Default;
        }
    }

    public ExpressionSyntax DefaultExpression( IFullRef<IType>? type )
        => type == null
            ? Default
            : SyntaxFactory.DefaultExpression( this.TypeSyntax( type ) )
                .WithSimplifierAnnotationIfNecessary( this.SyntaxGenerationContext );

    public ArrayCreationExpressionSyntax ArrayCreationExpression( TypeSyntax elementType, IEnumerable<SyntaxNode> elements )
    {
        var array = (ArrayCreationExpressionSyntax) _roslynSyntaxGenerator.ArrayCreationExpression( elementType, elements );

        return array.WithType( array.Type.WithSimplifierAnnotationIfNecessary( this.SyntaxGenerationContext ) )
            .NormalizeWhitespaceIfNecessary( this.SyntaxGenerationContext );
    }

    public TypeSyntax TypeSyntax( SpecialType specialType )
        => (TypeSyntax) _roslynSyntaxGenerator.TypeExpression( specialType )
            .WithSimplifierAnnotationIfNecessary( this.SyntaxGenerationContext );

    public CastExpressionSyntax CastExpression( IType targetType, ExpressionSyntax expression )
        => this.CastExpression( this.TypeSyntax( targetType ), expression );

    private CastExpressionSyntax CastExpression( TypeSyntax targetType, ExpressionSyntax expression ) => this.SafeCastExpression( targetType, expression );

    public TypeSyntax TypeOrNamespace( INamespaceOrTypeSymbol symbol )
    {
        TypeSyntax expression;

        switch ( symbol )
        {
            case ITypeSymbol typeSymbol:
                return this.TypeSyntax( typeSymbol );

            case INamespaceSymbol namespaceSymbol:
                expression = (NameSyntax) _roslynSyntaxGenerator.NameExpression( namespaceSymbol );

                break;

            default:
                throw new AssertionFailedException( $"Unexpected symbol kind: {symbol.Kind}." );
        }

        return expression.WithSimplifierAnnotationIfNecessary( this.SyntaxGenerationContext );
    }

    // ReSharper disable once MemberCanBeMadeStatic.Global
    public ThisExpressionSyntax ThisExpression() => (ThisExpressionSyntax) _roslynSyntaxGenerator.ThisExpression();

    // ReSharper disable once MemberCanBeMadeStatic.Global
    public IdentifierNameSyntax IdentifierName( string identifier ) => (IdentifierNameSyntax) _roslynSyntaxGenerator.IdentifierName( identifier );

    public TypeSyntax ArrayTypeExpression( TypeSyntax type )
    {
        var arrayType = (ArrayTypeSyntax) _roslynSyntaxGenerator.ArrayTypeExpression( type )
            .WithSimplifierAnnotationIfNecessary( this.SyntaxGenerationContext );

        // Roslyn does not specify the rank properly so it needs to be fixed up.

        return arrayType.WithRankSpecifiers( SingletonList( ArrayRankSpecifier( SingletonSeparatedList<ExpressionSyntax>( OmittedArraySizeExpression() ) ) ) );
    }

    public TypeSyntax ReturnType( IMethod method ) => this.TypeSyntax( method.ReturnType );

    public TypeSyntax PropertyType( IProperty property ) => this.TypeSyntax( property.Type );

    public TypeSyntax IndexerType( IIndexer indexer ) => this.TypeSyntax( indexer.Type );

    public TypeSyntax EventType( IEvent property ) => this.TypeSyntax( property.Type );

    // ReSharper disable once MemberCanBeMadeStatic.Global

#pragma warning disable CA1822 // Can be made static

    public ArgumentListSyntax ArgumentList( IMethodBase method, Func<IParameter, ExpressionSyntax?> expressionFunc )
        =>

            // TODO: optional parameters.
            SyntaxFactory.ArgumentList(
                SeparatedList(
                    method.Parameters.SelectAsImmutableArray(
                        p =>
                            Argument( expressionFunc( p ).AssertNotNull() ) ) ) );
#pragma warning restore CA1822 // Can be made static

    public SyntaxList<TypeParameterConstraintClauseSyntax> ConstraintClauses( IGeneric methodOrType )
    {
        List<TypeParameterConstraintClauseSyntax>? clauses = null;

        foreach ( var genericParameter in methodOrType.TypeParameters )
        {
            List<TypeParameterConstraintSyntax>? constraints = null;

            switch ( genericParameter.TypeKindConstraint )
            {
                case TypeKindConstraint.Class:
                    constraints ??= [];

                    var questionToken = genericParameter.IsConstraintNullable == true
                        ? Token( SyntaxKind.QuestionToken )
                        : default;

                    constraints.Add( ClassOrStructConstraint( SyntaxKind.ClassConstraint, Token( SyntaxKind.ClassKeyword ), questionToken ) );

                    break;

                case TypeKindConstraint.Struct:
                    constraints ??= [];
                    constraints.Add( ClassOrStructConstraint( SyntaxKind.StructConstraint ) );

                    break;

                case TypeKindConstraint.Unmanaged:
                    constraints ??= [];

                    constraints.Add(
                        TypeConstraint(
                            SyntaxFactory.IdentifierName( Identifier( default, SyntaxKind.UnmanagedKeyword, "unmanaged", "unmanaged", default ) ) ) );

                    break;

                case TypeKindConstraint.NotNull:
                    constraints ??= [];
                    constraints.Add( TypeConstraint( SyntaxFactory.IdentifierName( "notnull" ) ) );

                    break;

                case TypeKindConstraint.Default:
                    constraints ??= [];
                    constraints.Add( DefaultConstraint() );

                    break;
            }

            foreach ( var typeConstraint in genericParameter.TypeConstraints )
            {
                constraints ??= [];

                constraints.Add( TypeConstraint( this.TypeSyntax( typeConstraint ) ) );
            }

            if ( genericParameter.HasDefaultConstructorConstraint )
            {
                constraints ??= [];
                constraints.Add( ConstructorConstraint() );
            }

#if ROSLYN_4_12_0_OR_GREATER
            if ( genericParameter.AllowsRefStruct )
            {
                constraints ??= [];

                constraints.Add(
                    AllowsConstraintClause(
                        TokenWithTrailingSpace( SyntaxKind.AllowsKeyword ),
                        SingletonSeparatedList<AllowsConstraintSyntax>(
                            RefStructConstraint( TokenWithTrailingSpace( SyntaxKind.RefKeyword ), Token( SyntaxKind.StructKeyword ) ) ) ) );
            }
#endif

            if ( constraints != null )
            {
                constraints[^1] = constraints[^1].WithOptionalTrailingLineFeed( this.SyntaxGenerationContext );

                clauses ??= [];

                clauses.Add(
                    TypeParameterConstraintClause(
                        TokenWithTrailingSpace( SyntaxKind.WhereKeyword ),
                        SyntaxFactory.IdentifierName( genericParameter.Name ),
                        Token( SyntaxKind.ColonToken ),
                        SeparatedList( constraints ) ) );
            }
        }

        if ( clauses == null )
        {
            return default;
        }
        else
        {
            return List( clauses );
        }
    }

    public ExpressionSyntax EnumValueExpression( INamedTypeSymbol type, object value )
    {
        var member = type.GetMembers()
            .OfType<IFieldSymbol>()
            .FirstOrDefault( f => f is { IsConst: true, ConstantValue: not null } && f.ConstantValue.Equals( value ) );

        return this.EnumValueExpression( this.TypeSyntax( type ), value, member?.Name );
    }

    private ExpressionSyntax EnumValueExpression( INamedType type, object value )
    {
        if ( type.GetSymbol() is { } symbol )
        {
            return this.EnumValueExpression( symbol, value );
        }

        var member = type.Fields
            .FirstOrDefault( f => f is { Writeability: Writeability.None, ConstantValue.Value: { } constantValue } && constantValue.Equals( value ) );

        return this.EnumValueExpression( this.TypeSyntax( type ), value, member?.Name );
    }

    private ExpressionSyntax EnumValueExpression( TypeSyntax type, object value, string? memberName )
    {
        if ( memberName == null )
        {
            return this.CastExpression( type, LiteralExpression( value ) );
        }
        else
        {
            return
                MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        type,
                        this.IdentifierName( memberName ) )
                    .WithSimplifierAnnotationIfNecessary( this.SyntaxGenerationContext );
        }
    }

    public ExpressionSyntax TypedConstant( in TypedConstant typedConstant )
    {
        if ( typedConstant.IsNullOrDefault )
        {
            return this.DefaultExpression( typedConstant.Type );
        }
        else if ( typedConstant.Type is INamedType { TypeKind: TypeKind.Enum } enumType )
        {
            return this.EnumValueExpression( enumType, typedConstant.Value! );
        }
        else if ( typedConstant.IsArray )
        {
            var elementType = typedConstant.Type.AssertCast<IArrayType>().ElementType;

            return this.ArrayCreationExpression(
                this.TypeSyntax( elementType ),
                typedConstant.Values.SelectAsReadOnlyList( item => this.TypedConstant( item ) ) );
        }
        else
        {
            return LiteralExpression( typedConstant.Value! );
        }
    }

    public ExpressionSyntax TypedConstant( in TypedConstantRef typedConstant, RefFactory refFactory )
    {
        var type = typedConstant.Type?.ToFullRef( refFactory );

        if ( typedConstant.RawValue == null )
        {
            return this.DefaultExpression( type );
        }
        else if ( type?.Definition is INamedType { TypeKind: TypeKind.Enum } enumType )
        {
            return this.EnumValueExpression( enumType, typedConstant.RawValue! );
        }
        else if ( typedConstant.RawValue is Array array )
        {
            var elementType = type.AssertNotNull().AssertCast<IArrayType>().ElementType;

            return this.ArrayCreationExpression(
                this.TypeSyntax( elementType ),
                array.AsEnumerable<TypedConstantRef>().Select( item => this.TypedConstant( item, refFactory ) ) );
        }
        else
        {
            return LiteralExpression( typedConstant.RawValue! );
        }
    }

    // ReSharper disable once MemberCanBeMadeStatic.Global

#pragma warning disable CA1822
    public ExpressionSyntax RenderInterpolatedString( InterpolatedStringExpressionSyntax interpolatedString )
#pragma warning restore CA1822
    {
        List<InterpolatedStringContentSyntax> contents = new( interpolatedString.Contents.Count );

        foreach ( var content in interpolatedString.Contents )
        {
            switch ( content )
            {
                case InterpolatedStringTextSyntax text:
                    var previousIndex = contents.Count - 1;

                    if ( contents.Count > 0 && contents[previousIndex] is InterpolatedStringTextSyntax previousText )
                    {
                        // If we have two adjacent text tokens, we need to merge them, otherwise reformatting will add a white space.

                        var appendedText = previousText.TextToken.ValueText + text.TextToken.ValueText;

                        var escapedTextWithQuotes =
                            Literal( appendedText ).Text.ReplaceOrdinal( "{", "{{" ).ReplaceOrdinal( "}", "}}" );

                        var escapedText = escapedTextWithQuotes.Substring( 1, escapedTextWithQuotes.Length - 2 );

                        contents[previousIndex] = previousText.WithTextToken(
                            Token( default, SyntaxKind.InterpolatedStringTextToken, escapedText, appendedText, default ) );
                    }
                    else
                    {
                        contents.Add( text );
                    }

                    break;

                case InterpolationSyntax interpolation:
                    contents.Add( interpolation );

                    break;
            }
        }

        return interpolatedString.WithContents( List( contents ) );
    }

    public TypeSyntax TypeSyntax( IFullRef<IType> type )
        => type switch
        {
            ISymbolRef { SymbolMustBeMapped: false } symbolRef => this.TypeSyntax( (ITypeSymbol) symbolRef.Symbol ),
            _ => this.TypeSyntax( type.ConstructedDeclaration )
        };

    public TypeSyntax TypeSyntax( IType type, bool bypassSymbols = false )
    {
        if ( type is ISymbolBasedCompilationElement { SymbolMustBeMapped: false } symbolRef && !bypassSymbols )
        {
            return this.TypeSyntax( (ITypeSymbol) symbolRef.Symbol );
        }

        if ( this.SyntaxGenerationContext.HasCompilationContext && type.BelongsToCompilation( this.SyntaxGenerationContext.CompilationContext ) == true )
        {
            return this._typeSyntaxCache.AssertNotNull()
                .GetOrAdd(
                    type.ToRef(),
                    static ( _, x ) => x.This.TypeSyntaxCore( x.Type ),
                    (This: this, Type: type) );
        }
        else
        {
            return this.TypeSyntaxCore( type );
        }
    }

    public ExpressionSyntax TypeExpression( IType type, bool bypassSymbols = false )
    {
        if ( type.GetSymbol() is { } symbol && !bypassSymbols )
        {
            return this.TypeExpression( symbol );
        }

        if ( this.SyntaxGenerationContext.HasCompilationContext && type.BelongsToCompilation( this.SyntaxGenerationContext.CompilationContext ) == true )
        {
            return this._typeExpressionCache.AssertNotNull()
                .GetOrAdd(
                    type.ToRef(),
                    static ( _, x ) => x.This.TypeExpressionCore( x.Type ),
                    (This: this, Type: type) );
        }
        else
        {
            return this.TypeSyntaxCore( type );
        }
    }

    public TypeSyntax TypeSyntax( ITypeSymbol symbol )
    {
        if ( this.SyntaxGenerationContext.HasCompilationContext && symbol.BelongsToCompilation( this.SyntaxGenerationContext.CompilationContext ) == true )
        {
            return this._typeSymbolSyntaxCache.GetOrAdd(
                symbol,
                static ( _, x ) => x.This.TypeSyntaxCore( x.Type ),
                (This: this, Type: symbol) );
        }
        else
        {
            return this.TypeSyntaxCore( symbol );
        }
    }

    public ExpressionSyntax TypeExpression( ITypeSymbol symbol )
    {
        if ( this.SyntaxGenerationContext.HasCompilationContext && symbol.BelongsToCompilation( this.SyntaxGenerationContext.CompilationContext ) == true )
        {
            return this._typeSymbolExpressionCache.GetOrAdd(
                symbol,
                static ( _, x ) => x.This.TypeExpressionCore( x.Type ),
                (This: this, Type: symbol) );
        }
        else
        {
            return this.TypeSyntaxCore( symbol );
        }
    }

    private TypeSyntax TypeSyntaxCore( IType type )
    {
        var typeSyntax = this._syntaxGeneratorForIType.TypeSyntax( type ).WithSimplifierAnnotationIfNecessary( this.SyntaxGenerationContext );

        if ( !this.IsNullAware )
        {
            typeSyntax = (TypeSyntax) new RemoveReferenceNullableAnnotationsRewriter( type ).Visit( typeSyntax ).AssertNotNull();
        }

        if ( this.Options.TriviaMatters )
        {
            // Just calling NormalizeWhitespaceIfNecessary here produces ugly whitespace, e.g. "typeof(global::System.Int32[, ])".
            typeSyntax = (TypeSyntax) new NormalizeSpaceRewriter( this.SyntaxGenerationContext.EndOfLine ).Visit( typeSyntax ).AssertNotNull();
        }

        return typeSyntax;
    }

    private ExpressionSyntax TypeExpressionCore( IType type )
    {
        var typeSyntax = this._syntaxGeneratorForIType.TypeExpression( type ).WithSimplifierAnnotationIfNecessary( this.SyntaxGenerationContext );

        if ( !this.IsNullAware )
        {
            typeSyntax = (ExpressionSyntax) new RemoveReferenceNullableAnnotationsRewriter( type ).Visit( typeSyntax ).AssertNotNull();
        }

        if ( this.Options.TriviaMatters )
        {
            // Just calling NormalizeWhitespaceIfNecessary here produces ugly whitespace, e.g. "typeof(global::System.Int32[, ])".
            typeSyntax = (ExpressionSyntax) new NormalizeSpaceRewriter( this.SyntaxGenerationContext.EndOfLine ).Visit( typeSyntax ).AssertNotNull();
        }

        return typeSyntax;
    }

    private TypeSyntax TypeSyntaxCore( ITypeSymbol symbol )
    {
        var typeSyntax = (TypeSyntax) _roslynSyntaxGenerator.TypeExpression( symbol ).WithSimplifierAnnotationIfNecessary( this.SyntaxGenerationContext );

        if ( !this.IsNullAware )
        {
            typeSyntax = (TypeSyntax) new RemoveReferenceNullableAnnotationsRewriterForSymbol( symbol ).Visit( typeSyntax ).AssertNotNull();
        }

        // We always need to normalize whitespace in tuples to workaround a Roslyn bug.
        typeSyntax = (TypeSyntax) new NormalizeSpaceRewriter( this.SyntaxGenerationContext.EndOfLine ).Visit( typeSyntax ).AssertNotNull();

        return typeSyntax;
    }

    private TypeSyntax TypeExpressionCore( ITypeSymbol symbol )
    {
        if ( symbol is { IsReferenceType: true, NullableAnnotation: NullableAnnotation.Annotated } )
        {
            symbol = symbol.WithNullableAnnotation( NullableAnnotation.NotAnnotated );
        }

        var typeSyntax = (TypeSyntax) _roslynSyntaxGenerator.TypeExpression( symbol ).WithSimplifierAnnotationIfNecessary( this.SyntaxGenerationContext );

        if ( !this.IsNullAware )
        {
            typeSyntax = (TypeSyntax) new RemoveReferenceNullableAnnotationsRewriterForSymbol( symbol ).Visit( typeSyntax ).AssertNotNull();
        }

        // We always need to normalize whitespace in tuples to workaround a Roslyn bug.
        typeSyntax = (TypeSyntax) new NormalizeSpaceRewriter( this.SyntaxGenerationContext.EndOfLine ).Visit( typeSyntax ).AssertNotNull();

        return typeSyntax;
    }

    private SyntaxGenerationOptions Options => this.SyntaxGenerationContext.Options;

    public AttributeSyntax Attribute( IAttributeData attribute )
    {
        var lastParameter = attribute.Constructor.Parameters.LastOrDefault();

        IEnumerable<AttributeArgumentSyntax> constructorArguments;

        if ( lastParameter is { IsParams: true }
             && attribute.ConstructorArguments[^1] is { Values.IsDefault: false } lastArray )
        {
            // Use the more compact syntax.
            var constructorArgumentValues = attribute.ConstructorArguments.ToMutableList();
            constructorArgumentValues.RemoveAt( constructorArgumentValues.Count - 1 );
            constructorArgumentValues.AddRange( lastArray.Values );
            constructorArguments = constructorArgumentValues.SelectAsReadOnlyCollection( a => AttributeArgument( this.TypedConstantExpression( a ) ) );
        }
        else
        {
            constructorArguments = attribute.ConstructorArguments.Select( a => AttributeArgument( this.TypedConstantExpression( a ) ) );
        }

        var namedArguments = attribute.NamedArguments.SelectAsImmutableArray(
            a => AttributeArgument(
                NameEquals( a.Key ),
                null,
                this.TypedConstantExpression( a.Value ) ) );

        var attributeSyntax = SyntaxFactory.Attribute( (NameSyntax) this.TypeSyntax( attribute.Type ) );

        var argumentList = AttributeArgumentList( SeparatedList( constructorArguments.Concat( namedArguments ) ) );

        if ( argumentList.Arguments.Count > 0 )
        {
            // Add the argument list only when it is non-empty, otherwise this generates redundant parenthesis.
            attributeSyntax = attributeSyntax.WithArgumentList( argumentList );
        }

        return attributeSyntax;
    }

    public SyntaxList<AttributeListSyntax> AttributesForDeclaration(
        IFullRef<IDeclaration> declaration,
        CompilationModel compilation,
        SyntaxKind attributeTargetKind = SyntaxKind.None )
    {
        var attributes = compilation.GetAttributeCollection( declaration );

        if ( attributes.Count == 0 )
        {
            return default;
        }
        else
        {
            var list = new List<AttributeListSyntax>();

            foreach ( var attribute in attributes )
            {
                if ( attribute.GetTarget( compilation ).Constructor.DeclaringType.FullName == "System.Runtime.CompilerServices.NullableContextAttribute" )
                {
                    continue;
                }

                var attributeList = AttributeList( SingletonSeparatedList( this.Attribute( attribute.GetTarget( compilation ) ) ) );

                if ( attributeTargetKind != SyntaxKind.None )
                {
                    attributeList = attributeList.WithTarget( AttributeTargetSpecifier( Token( attributeTargetKind ) ) );
                }

                list.Add( attributeList );
            }

            return List( list );
        }
    }

    public SyntaxNode AddAttribute( SyntaxNode oldNode, IAttributeData attribute )
    {
        var attributeList = AttributeList( SingletonSeparatedList( this.Attribute( attribute ) ) )
            .WithOptionalLeadingTrivia( oldNode.GetLeadingTrivia(), this.SyntaxGenerationContext.Options )
            .WithOptionalTrailingLineFeed( this.SyntaxGenerationContext );

        oldNode = oldNode.WithOptionalLeadingTrivia( default(SyntaxTriviaList), this.SyntaxGenerationContext.Options );

        if ( attributeList.GetLeadingTrivia().LastOrDefault() is { RawKind: (int) SyntaxKind.WhitespaceTrivia } indentationTrivia )
        {
            oldNode = oldNode.WithOptionalLeadingTrivia( indentationTrivia, this.SyntaxGenerationContext.Options );
        }

        return oldNode.Kind() switch
        {
            SyntaxKind.MethodDeclaration => ((MethodDeclarationSyntax) oldNode).AddAttributeLists( attributeList ),
            SyntaxKind.DestructorDeclaration => ((DestructorDeclarationSyntax) oldNode).AddAttributeLists( attributeList ),
            SyntaxKind.ConstructorDeclaration => ((ConstructorDeclarationSyntax) oldNode).AddAttributeLists( attributeList ),
            SyntaxKind.InterfaceDeclaration => ((InterfaceDeclarationSyntax) oldNode).AddAttributeLists( attributeList ),
            SyntaxKind.DelegateDeclaration => ((DelegateDeclarationSyntax) oldNode).AddAttributeLists( attributeList ),
            SyntaxKind.EnumDeclaration => ((EnumDeclarationSyntax) oldNode).AddAttributeLists( attributeList ),
            SyntaxKind.ClassDeclaration => ((ClassDeclarationSyntax) oldNode).AddAttributeLists( attributeList ),
            SyntaxKind.StructDeclaration => ((StructDeclarationSyntax) oldNode).AddAttributeLists( attributeList ),
            SyntaxKind.Parameter => ((ParameterSyntax) oldNode).AddAttributeLists( attributeList ),
            SyntaxKind.PropertyDeclaration => ((PropertyDeclarationSyntax) oldNode).AddAttributeLists( attributeList ),
            SyntaxKind.EventDeclaration => ((EventDeclarationSyntax) oldNode).AddAttributeLists( attributeList ),
            SyntaxKind.AddAccessorDeclaration => ((AccessorDeclarationSyntax) oldNode).AddAttributeLists( attributeList ),
            SyntaxKind.RemoveAccessorDeclaration => ((AccessorDeclarationSyntax) oldNode).AddAttributeLists( attributeList ),
            SyntaxKind.GetAccessorDeclaration => ((AccessorDeclarationSyntax) oldNode).AddAttributeLists( attributeList ),
            SyntaxKind.SetAccessorDeclaration => ((AccessorDeclarationSyntax) oldNode).AddAttributeLists( attributeList ),
            SyntaxKind.OperatorDeclaration => ((OperatorDeclarationSyntax) oldNode).AddAttributeLists( attributeList ),
            SyntaxKind.ConversionOperatorDeclaration => ((ConversionOperatorDeclarationSyntax) oldNode).AddAttributeLists( attributeList ),
            SyntaxKind.IndexerDeclaration => ((IndexerDeclarationSyntax) oldNode).AddAttributeLists( attributeList ),
            SyntaxKind.FieldDeclaration => ((FieldDeclarationSyntax) oldNode).AddAttributeLists( attributeList ),
            SyntaxKind.EventFieldDeclaration => ((EventFieldDeclarationSyntax) oldNode).AddAttributeLists( attributeList ),
            _ => throw new AssertionFailedException( $"Unexpected syntax kind {oldNode.Kind()} at '{oldNode.GetLocation()}'." )
        };
    }

    public ExpressionSyntax TypedConstantExpression( TypedConstant typedConstant, IType? targetType = null )
    {
        if ( typedConstant.IsNullOrDefault )
        {
            return this.DefaultExpression( typedConstant.Type, targetType );
        }

        ExpressionSyntax GetValue( object? value, IType type )
        {
            if ( value is TypedConstant innerTypedConstant )
            {
                value = innerTypedConstant.Value;
            }

            if ( value == null )
            {
                return Null;
            }

            switch ( type )
            {
                case INamedType { TypeKind: TypeKind.Enum } enumType:
                    return this.EnumValueExpression( enumType, value );

                case IArrayType arrayType:
                    return this.ArrayCreationExpression(
                        this.TypeSyntax( arrayType.ElementType ),
                        ((ImmutableArray<TypedConstant>) value).Select( x => GetValue( x.RawValue, x.Type ) ) );

                default:
                    switch ( value )
                    {
                        case IType typeValue:
                            return this.TypeOfExpression( typeValue );

                        case Type systemTypeValue:
                            return this.TypeOfExpression( this.SyntaxGenerationContext.ReflectionMapper.GetTypeSymbol( systemTypeValue ) );

                        default:
                            {
                                var literal = LiteralExpressionOrNull( value );

                                if ( literal != null )
                                {
                                    return literal;
                                }
                                else
                                {
                                    throw new ArgumentOutOfRangeException(
                                        nameof(value),
                                        $"The value '{value}' cannot be converted to a custom attribute argument value." );
                                }
                            }
                    }
            }
        }

        return GetValue( typedConstant.RawValue, typedConstant.Type );
    }

    public TypeParameterListSyntax? TypeParameterList( IMethod method, CompilationModel compilation )
    {
        if ( method.TypeParameters.Count == 0 )
        {
            return null;
        }
        else
        {
            var list = SyntaxFactory.TypeParameterList(
                SeparatedList( method.TypeParameters.SelectAsImmutableArray( p => this.TypeParameter( p, compilation ) ) ) );

            return list;
        }
    }

    private TypeParameterSyntax TypeParameter( ITypeParameter typeParameter, CompilationModel compilation )
    {
        var syntax = SyntaxFactory.TypeParameter( typeParameter.Name );

        switch ( typeParameter.Variance )
        {
            case VarianceKind.In:
                syntax = syntax.WithVarianceKeyword( Token( SyntaxKind.InKeyword ) );

                break;

            case VarianceKind.Out:
                syntax = syntax.WithVarianceKeyword( Token( SyntaxKind.OutKeyword ) );

                break;
        }

        syntax = syntax.WithAttributeLists( this.AttributesForDeclaration( typeParameter.ToFullRef(), compilation ) );

        return syntax;
    }

    public ParameterListSyntax ParameterList( IMethodBase method, CompilationModel compilation, bool removeDefaultValues = false )
        => SyntaxFactory.ParameterList( this.ParameterListParameters( method, compilation, removeDefaultValues ) );

    public ParameterListSyntax ParameterList( IReadOnlyList<IParameter> parameters, CompilationModel compilation, bool removeDefaultValues = false )
        => SyntaxFactory.ParameterList( this.ParameterListParameters( parameters, compilation, removeDefaultValues ) );

    public BracketedParameterListSyntax ParameterList( IIndexer indexer, CompilationModel compilation, bool removeDefaultValues = false )
        => BracketedParameterList( this.ParameterListParameters( indexer, compilation, removeDefaultValues ) );

    private SeparatedSyntaxList<ParameterSyntax> ParameterListParameters( IHasParameters method, CompilationModel compilation, bool removeDefaultValues )
        => this.ParameterListParameters( method.Parameters, compilation, removeDefaultValues );

    private SeparatedSyntaxList<ParameterSyntax> ParameterListParameters(
        IReadOnlyList<IParameter> parameters,
        CompilationModel compilation,
        bool removeDefaultValues )
        => SeparatedList( parameters.SelectAsReadOnlyList( p => this.Parameter( p, compilation, removeDefaultValues ) ) );

    public ParameterSyntax Parameter( IParameter parameter, CompilationModel compilation, bool removeDefaultValue )
    {
        // We intentionally generate non-literal values to be more tolerant to invalid inputs.
        // Also, it's required to generate the correct syntax for enum values (other than 0).
        var equalsValueClause = removeDefaultValue || parameter.DefaultValue == null
            ? null
            : EqualsValueClause( this.TypedConstantExpression( parameter.DefaultValue.Value, parameter.Type ) );

        return SyntaxFactory.Parameter(
            this.AttributesForDeclaration( parameter.ToFullRef(), compilation ),
            parameter.GetSyntaxModifierList(),
            this.TypeSyntax( parameter.Type ).WithOptionalTrailingTrivia( ElasticSpace, this.Options ),
            Identifier( parameter.Name ),
            equalsValueClause );
    }

    public SyntaxList<TypeParameterConstraintClauseSyntax> TypeParameterConstraintClauses( ImmutableArray<ITypeParameterSymbol> typeParameters )
    {
        // Spec: https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/generics/constraints-on-type-parameters

        if ( typeParameters.IsDefaultOrEmpty )
        {
            return default;
        }

        var list = List<TypeParameterConstraintClauseSyntax>();

        foreach ( var parameter in typeParameters )
        {
            var constraints = SeparatedList<TypeParameterConstraintSyntax>();

            if ( parameter.HasNotNullConstraint )
            {
                constraints = constraints.Add( TypeConstraint( SyntaxFactory.IdentifierName( "notnull" ) ) );
            }
            else if ( parameter.HasReferenceTypeConstraint )
            {
                constraints =
                    constraints.Add(
                        parameter.ReferenceTypeConstraintNullableAnnotation != NullableAnnotation.Annotated
                            ? ClassOrStructConstraint( SyntaxKind.ClassConstraint )
                            : ClassOrStructConstraint( SyntaxKind.ClassConstraint ).WithQuestionToken( Token( SyntaxKind.QuestionToken ) ) );
            }
            else if ( parameter.HasValueTypeConstraint )
            {
                if ( parameter.HasUnmanagedTypeConstraint )
                {
                    constraints = constraints.Add(
                        TypeConstraint(
                            SyntaxFactory.IdentifierName(
                                Identifier(
                                    default,
                                    SyntaxKind.UnmanagedKeyword,
                                    "unmanaged",
                                    "unmanaged",
                                    default ) ) ) );
                }
                else
                {
                    constraints = constraints.Add( ClassOrStructConstraint( SyntaxKind.StructConstraint ) );
                }
            }

            foreach ( var typeConstraint in parameter.ConstraintTypes )
            {
                constraints = constraints.Add( TypeConstraint( this.TypeSyntax( typeConstraint ) ) );
            }

            if ( parameter.HasConstructorConstraint )
            {
                constraints = constraints.Add( ConstructorConstraint() );
            }

            if ( constraints.Count > 0 )
            {
                var clause = TypeParameterConstraintClause( parameter.Name )
                    .WithConstraints( constraints )
                    .NormalizeWhitespaceIfNecessary( this.SyntaxGenerationContext );

                list = list.Add( clause );
            }
        }

        return list;
    }

    public CastExpressionSyntax SafeCastExpression( TypeSyntax type, ExpressionSyntax syntax )
    {
        if ( syntax is CastExpressionSyntax cast && cast.Type.IsEquivalentTo( type, topLevel: false ) )
        {
            // It's already a cast to the same type, no need to cast again.
            return cast;
        }

        var requiresParenthesis = syntax switch
        {
            CastExpressionSyntax => false,
            InvocationExpressionSyntax => false,
            MemberAccessExpressionSyntax => false,
            ElementAccessExpressionSyntax => false,
            IdentifierNameSyntax => false,
            LiteralExpressionSyntax => false,
            DefaultExpressionSyntax => false,
            TypeOfExpressionSyntax => false,
            ParenthesizedExpressionSyntax => false,
            ConditionalAccessExpressionSyntax => false,
            ObjectCreationExpressionSyntax => false,
            ArrayCreationExpressionSyntax => false,
            PostfixUnaryExpressionSyntax => false,

#if ROSLYN_4_8_0_OR_GREATER
            CollectionExpressionSyntax => false,
#endif

            // The syntax (T)-x is ambiguous and interpreted as binary minus, not cast of unary minus.
            PrefixUnaryExpressionSyntax { RawKind: not (int) SyntaxKind.UnaryMinusExpression } => false,
            TupleExpressionSyntax => false,
            ThisExpressionSyntax => false,
            _ => true
        };

        if ( requiresParenthesis )
        {
            return SyntaxFactory.CastExpression( type, ParenthesizedExpression( syntax ).WithSimplifierAnnotationIfNecessary( this.SyntaxGenerationContext ) )
                .WithSimplifierAnnotationIfNecessary( this.SyntaxGenerationContext );
        }
        else
        {
            return SyntaxFactory.CastExpression( type, syntax ).WithSimplifierAnnotationIfNecessary( this.SyntaxGenerationContext );
        }
    }

    public BlockSyntax FormattedBlock() => this.MemoizedFormattedBlock;

    [Memo]
    private BlockSyntax MemoizedFormattedBlock => this.FormattedBlock( [] );

    public BlockSyntax FormattedBlock( params StatementSyntax[] statements ) => this.FormattedBlock( (IEnumerable<StatementSyntax>) statements );

    private static bool NeedsLineFeed( StatementSyntax statement )
        => !statement.HasTrailingTrivia || !statement.GetTrailingTrivia()[^1].IsKind( SyntaxKind.EndOfLineTrivia );

    public BlockSyntax FormattedBlock( IEnumerable<StatementSyntax> statements )
        => Block(
            Token( default, SyntaxKind.OpenBraceToken, this.SyntaxGenerationContext.ElasticEndOfLineTriviaList ),
            List(
                statements.Select(
                    s => NeedsLineFeed( s )
                        ? s.WithOptionalTrailingLineFeed( this.SyntaxGenerationContext )
                        : s ) ),
            Token( this.SyntaxGenerationContext.ElasticEndOfLineTriviaList, SyntaxKind.CloseBraceToken, default ) );

    public ExpressionSyntax SuppressNullableWarningExpression( ExpressionSyntax operand, IType? operandType )
    {
        var isSuppressionEnabled = false;

        if ( this.IsNullAware )
        {
            isSuppressionEnabled = true;

            if ( operandType != null )
            {
                if ( operand.Kind() is SyntaxKind.NullLiteralExpression or SyntaxKind.DefaultLiteralExpression )
                {
                    // ! is required when assigning null to a non-nullable.
                    if ( operandType is { IsReferenceType: true, IsNullable: true } )
                    {
                        isSuppressionEnabled = false;
                    }
                }
                else if ( operandType.IsReferenceType == false )
                {
                    // Value types, including nullable value types don't need suppression.
                    isSuppressionEnabled = false;
                }
                else
                {
                    var nullable = operandType.IsNullable;

                    if ( nullable == false || (nullable == null && operandType.TypeKind != TypeKind.TypeParameter) )
                    {
                        // Non-nullable and non-annotated types don't need suppression, except generic type parameters that are not explicitly marked as non nullable.
                        isSuppressionEnabled = false;
                    }
                }
            }
        }

        return isSuppressionEnabled
            ? PostfixUnaryExpression( SyntaxKind.SuppressNullableWarningExpression, operand ).WithSimplifierAnnotation()
            : operand;
    }
}