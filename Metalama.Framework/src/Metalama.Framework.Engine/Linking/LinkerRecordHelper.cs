// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Linking.Substitution;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using static Metalama.Framework.Engine.SyntaxGeneration.SyntaxFactoryEx;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking;

/// <summary>
/// Helper class for handling compiler-synthesized record members (e.g. Equals, GetHashCode, EqualityContract)
/// that need to be overridden by aspects but have no explicit syntax in source.
/// </summary>
internal sealed class LinkerRecordHelper
{
    private readonly LinkerRewritingDriver _rewritingDriver;

    public LinkerRecordHelper( LinkerRewritingDriver rewritingDriver )
    {
        this._rewritingDriver = rewritingDriver;
    }

    /// <summary>
    /// Gets synthesized override target methods for a type (e.g. record-synthesized Equals, GetHashCode).
    /// These are methods that are override targets but have no explicit syntax in source.
    /// </summary>
    public IEnumerable<IMethodSymbol> GetSynthesizedMethodOverrideTargets( INamedTypeSymbol typeSymbol )
    {
        var methods = new List<IMethodSymbol>();

        foreach ( var overriddenMember in this._rewritingDriver.InjectionRegistry.GetOverriddenMembers() )
        {
            if ( overriddenMember.Kind == SymbolKind.Method
                 && overriddenMember is IMethodSymbol methodSymbol
                 && this._rewritingDriver.IntermediateCompilationContext.SymbolComparer.Equals( methodSymbol.ContainingType, typeSymbol )
                 && methodSymbol.IsImplicitlyDeclared
                 && methodSymbol.GetPrimaryDeclarationSyntax()?.Kind() is SyntaxKind.RecordDeclaration or SyntaxKind.RecordStructDeclaration )
            {
                methods.Add( methodSymbol );
            }
        }

        // The overridden members are not enumerated in a stable order, and these members have no declaration in source
        // whose position could order them, so they are sorted by signature to keep the output of the compilation deterministic.
        return methods.OrderBy( m => m.ToDisplayString(), StringComparer.Ordinal );
    }

    /// <summary>
    /// Gets synthesized override target properties for a type (e.g. record-synthesized EqualityContract).
    /// These are properties that are override targets but have no explicit syntax in source.
    /// </summary>
    public IEnumerable<IPropertySymbol> GetSynthesizedPropertyOverrideTargets( INamedTypeSymbol typeSymbol )
    {
        var properties = new List<IPropertySymbol>();

        foreach ( var overriddenMember in this._rewritingDriver.InjectionRegistry.GetOverriddenMembers() )
        {
            if ( overriddenMember.Kind == SymbolKind.Property
                 && overriddenMember is IPropertySymbol propertySymbol
                 && this._rewritingDriver.IntermediateCompilationContext.SymbolComparer.Equals( propertySymbol.ContainingType, typeSymbol )
                 && propertySymbol.IsImplicitlyDeclared
                 && (propertySymbol.GetPrimaryDeclarationSyntax() ?? propertySymbol.ContainingType?.GetPrimaryDeclarationSyntax())?.Kind()
                 is SyntaxKind.RecordDeclaration or SyntaxKind.RecordStructDeclaration )
            {
                properties.Add( propertySymbol );
            }
        }

        // See the comment in GetSynthesizedMethodOverrideTargets about the order.
        return properties.OrderBy( p => p.ToDisplayString(), StringComparer.Ordinal );
    }

    /// <summary>
    /// Rewrites a synthesized override target method (e.g. record-synthesized Equals) that has no explicit syntax in source.
    /// Creates a method declaration from the symbol and applies the linked body.
    /// </summary>
    public IReadOnlyList<MemberDeclarationSyntax> RewriteSynthesizedMethodOverrideTarget(
        IMethodSymbol symbol,
        SyntaxGenerationContext generationContext )
    {
        Invariant.Assert( this._rewritingDriver.InjectionRegistry.IsOverrideTarget( symbol ) );

        var members = new List<MemberDeclarationSyntax>();
        var lastOverride = this._rewritingDriver.InjectionRegistry.GetLastOverride( symbol );

        // Create a synthetic MethodDeclarationSyntax from the symbol.
        var returnType = generationContext.SyntaxGenerator.TypeSyntax( symbol.ReturnType )
            .WithOptionalTrailingTrivia( ElasticSpace, generationContext.Options );

        var parameterList = ParameterList(
            SeparatedList(
                symbol.Parameters.SelectAsReadOnlyList(
                    p =>
                    {
                        var parameterSyntax = Parameter(
                            default,
                            default,
                            generationContext.SyntaxGenerator.TypeSyntax( p.Type )
                                .WithOptionalTrailingTrivia( ElasticSpace, generationContext.Options ),
                            SafeIdentifier( p.Name ),
                            null );

                        if ( p.RefKind != RefKind.None )
                        {
                            if ( p.RefKind == RefKind.RefReadOnlyParameter )
                            {
                                parameterSyntax = parameterSyntax.WithModifiers(
                                    TokenList(
                                        Token( default, SyntaxKind.RefKeyword, TriviaList( ElasticSpace ) ),
                                        Token( default, SyntaxKind.ReadOnlyKeyword, TriviaList( ElasticSpace ) ) ) );
                            }
                            else
                            {
                                var refKindKeyword = p.RefKind switch
                                {
                                    RefKind.Ref => SyntaxKind.RefKeyword,
                                    RefKind.Out => SyntaxKind.OutKeyword,
                                    RefKind.In => SyntaxKind.InKeyword,
                                    _ => throw new AssertionFailedException( $"Unexpected RefKind: {p.RefKind}." )
                                };

                                parameterSyntax = parameterSyntax.WithModifiers( TokenList( Token( default, refKindKeyword, TriviaList( ElasticSpace ) ) ) );
                            }
                        }

                        return parameterSyntax;
                    } ) ) );

        var modifiers = symbol.GetSyntaxModifierList(
            ModifierCategories.Accessibility
            | ModifierCategories.Static
            | ModifierCategories.Unsafe
            | ModifierCategories.Inheritance
            | ModifierCategories.ReadOnly );

        var typeParameterList = symbol.TypeParameters.Length > 0
            ? TypeParameterList( SeparatedList( symbol.TypeParameters.SelectAsReadOnlyList( tp => TypeParameter( SafeIdentifier( tp.Name ) ) ) ) )
            : null;

        var syntheticMethodDeclaration = MethodDeclaration(
            List<AttributeListSyntax>(),
            modifiers,
            returnType,
            null,
            SafeIdentifier( symbol.Name ),
            typeParameterList,
            parameterList,
            List<TypeParameterConstraintClauseSyntax>(),
            null,
            null );

        if ( this._rewritingDriver.AnalysisRegistry.IsInlined( lastOverride.ToSemantic( IntermediateSymbolSemanticKind.Default ) ) )
        {
            members.Add( GetLinkedDeclaration( IntermediateSymbolSemanticKind.Final, lastOverride.IsAsync ) );
        }
        else
        {
            members.Add(
                this.GetTrampolineForSynthesizedMethod(
                    syntheticMethodDeclaration,
                    lastOverride.ToSemantic( IntermediateSymbolSemanticKind.Default ),
                    generationContext ) );
        }

        if ( this._rewritingDriver.AnalysisRegistry.IsReachable( symbol.ToSemantic( IntermediateSymbolSemanticKind.Default ) )
             && !this._rewritingDriver.AnalysisRegistry.IsInlined( symbol.ToSemantic( IntermediateSymbolSemanticKind.Default ) )
             && this._rewritingDriver.ShouldGenerateSourceMember( symbol ) )
        {
            members.Add( this.GetOriginalImplMethod( syntheticMethodDeclaration, symbol, generationContext ) );
        }

        if ( this._rewritingDriver.AnalysisRegistry.IsReachable( symbol.ToSemantic( IntermediateSymbolSemanticKind.Base ) )
             && !this._rewritingDriver.AnalysisRegistry.IsInlined( symbol.ToSemantic( IntermediateSymbolSemanticKind.Base ) )
             && this._rewritingDriver.ShouldGenerateEmptyMember( symbol ) )
        {
            members.Add( this._rewritingDriver.GetEmptyImplMethod( syntheticMethodDeclaration, symbol, generationContext ) );
        }

        return members;

        MethodDeclarationSyntax GetLinkedDeclaration( IntermediateSymbolSemanticKind semanticKind, bool isAsync )
        {
            var linkedBody = this._rewritingDriver.GetSubstitutedBody(
                symbol.ToSemantic( semanticKind ),
                new SubstitutionContext(
                    this._rewritingDriver,
                    generationContext,
                    new InliningContextIdentifier( symbol.ToSemantic( semanticKind ) ) ) );

            var currentModifiers = syntheticMethodDeclaration.Modifiers;

            if ( isAsync && !symbol.IsAsync )
            {
                currentModifiers = currentModifiers.Add( Token( TriviaList( ElasticSpace ), SyntaxKind.AsyncKeyword, TriviaList( ElasticSpace ) ) );
            }

            return syntheticMethodDeclaration
                .PartialUpdate(
                    modifiers: currentModifiers,
                    body: Block(
                            Token( TriviaList( ElasticMarker ), SyntaxKind.OpenBraceToken, TriviaList( ElasticMarker ) ),
                            SingletonList<StatementSyntax>( linkedBody ),
                            Token( TriviaList( ElasticMarker ), SyntaxKind.CloseBraceToken, TriviaList( ElasticMarker ) ) )
                        .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock )
                        .WithGeneratedCodeAnnotation( FormattingAnnotations.SystemGeneratedCodeAnnotation ) )
                .WithOptionalLeadingAndTrailingLineFeed( generationContext )
                .WithGeneratedCodeAnnotation( FormattingAnnotations.SystemGeneratedCodeAnnotation );
        }
    }

    /// <summary>
    /// Creates the source impl method for a synthesized override target, which is what <c>meta.Proceed()</c> calls when the
    /// original implementation is not inlined. Its body is the one that the compiler would have synthesized, or, for the
    /// members that cannot be reproduced in source, a statement that throws <see cref="System.NotSupportedException"/>.
    /// </summary>
    private MemberDeclarationSyntax GetOriginalImplMethod(
        MethodDeclarationSyntax method,
        IMethodSymbol symbol,
        SyntaxGenerationContext generationContext )
    {
        var body = SynthesizedRecordMemberBodyGenerator.GetMemberKind( symbol ) != SynthesizedRecordMemberKind.None
            ? this.GetMaterializedBody( symbol, generationContext )
            : GetNotSupportedBody( generationContext );

        return this._rewritingDriver.GetSpecialImplMethod(
            method,
            body,
            null,
            symbol,
            LinkerRewritingDriver.GetOriginalImplMemberName( symbol ),
            generationContext );
    }

    /// <summary>
    /// Generates the body that the compiler would have synthesized for a record member.
    /// </summary>
    private BlockSyntax GetMaterializedBody( IMethodSymbol symbol, SyntaxGenerationContext generationContext )
    {
        var (statements, result) = SynthesizedRecordMemberBodyGenerator.GenerateBody( symbol, this._rewritingDriver, generationContext );

        var allStatements = new List<StatementSyntax>( statements );

        if ( result != null )
        {
            allStatements.Add(
                ReturnStatement(
                    TokenWithTrailingSpace( SyntaxKind.ReturnKeyword ),
                    result,
                    Token( SyntaxKind.SemicolonToken ) ) );
        }

        return generationContext.SyntaxGenerator.FormattedBlock( allStatements );
    }

    private static BlockSyntax GetNotSupportedBody( SyntaxGenerationContext generationContext )
        => generationContext.SyntaxGenerator.FormattedBlock(
                ThrowStatement(
                    TokenWithTrailingSpace( SyntaxKind.ThrowKeyword ),
                    ObjectCreationExpression(
                        TokenWithTrailingSpace( SyntaxKind.NewKeyword ),
                        QualifiedName(
                            AliasQualifiedName(
                                WellKnownIdentifierName( Token( SyntaxKind.GlobalKeyword ) ),
                                WellKnownIdentifierName( "System" ) ),
                            WellKnownIdentifierName( "NotSupportedException" ) ),
                        ArgumentList(
                            SingletonSeparatedList(
                                Argument(
                                    LiteralExpression(
                                        SyntaxKind.StringLiteralExpression,
                                        Literal(
                                            "Calling the original implementation of this compiler-synthesized record member is not supported, because the member cannot be declared explicitly. Override Equals(R) instead, which the equality operators and Equals(object) both call." ) ) ) ) ),
                        null ),
                    Token( SyntaxKind.SemicolonToken ) ) );

    private MethodDeclarationSyntax GetTrampolineForSynthesizedMethod(
        MethodDeclarationSyntax method,
        IntermediateSymbolSemantic<IMethodSymbol> targetSemantic,
        SyntaxGenerationContext context )
    {
        return method
            .PartialUpdate( body: GetBody() )
            .WithTriviaFromIfNecessary( method, this._rewritingDriver.SyntaxGenerationOptions );

        BlockSyntax GetBody()
        {
            var invocation =
                InvocationExpression(
                    GetInvocationTarget(),
                    ArgumentList(
                        SeparatedList( method.ParameterList.Parameters.SelectAsReadOnlyList( x => Argument( WellKnownIdentifierName( x.Identifier ) ) ) ) ) );

            if ( !targetSemantic.Symbol.ReturnsVoid )
            {
                return context.SyntaxGenerator.FormattedBlock(
                    ReturnStatement(
                        TokenWithTrailingSpace( SyntaxKind.ReturnKeyword ),
                        invocation,
                        Token( SyntaxKind.SemicolonToken ) ) );
            }
            else
            {
                return context.SyntaxGenerator.FormattedBlock( ExpressionStatement( invocation ) );
            }

            ExpressionSyntax GetInvocationTarget()
            {
                if ( targetSemantic.Symbol.IsStatic )
                {
                    return SafeIdentifierName( targetSemantic.Symbol.Name );
                }
                else
                {
                    return MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression, ThisExpression(), SafeIdentifierName( targetSemantic.Symbol.Name ) )
                        .WithSimplifierAnnotationIfNecessary( context );
                }
            }
        }
    }

    /// <summary>
    /// Rewrites a synthesized override target property (e.g. record-synthesized EqualityContract) that has no explicit syntax in source.
    /// Creates a property declaration from the symbol and applies the linked body.
    /// </summary>
    public IReadOnlyList<MemberDeclarationSyntax> RewriteSynthesizedPropertyOverrideTarget(
        IPropertySymbol symbol,
        SyntaxGenerationContext generationContext )
    {
        Invariant.Assert( this._rewritingDriver.InjectionRegistry.IsOverrideTarget( symbol ) );

        var members = new List<MemberDeclarationSyntax>();
        var lastOverride = (IPropertySymbol) this._rewritingDriver.InjectionRegistry.GetLastOverride( symbol );

        // Create a synthetic PropertyDeclarationSyntax from the symbol.
        var propertyType = generationContext.SyntaxGenerator.TypeSyntax( symbol.Type )
            .WithOptionalTrailingTrivia( ElasticSpace, generationContext.Options );

        var modifiers = symbol.GetSyntaxModifierList(
            ModifierCategories.Accessibility
            | ModifierCategories.Static
            | ModifierCategories.Unsafe
            | ModifierCategories.Inheritance
            | ModifierCategories.ReadOnly );

        // Build accessor list from the symbol.
        var accessors = new List<AccessorDeclarationSyntax>();

        if ( symbol.GetMethod != null )
        {
            accessors.Add(
                AccessorDeclaration(
                    SyntaxKind.GetAccessorDeclaration,
                    List<AttributeListSyntax>(),
                    TokenList(),
                    Token( SyntaxKind.GetKeyword ),
                    null,
                    null,
                    Token( SyntaxKind.SemicolonToken ) ) );
        }

        if ( symbol.SetMethod != null )
        {
            var setKind = symbol.SetMethod.IsInitOnly
                ? SyntaxKind.InitAccessorDeclaration
                : SyntaxKind.SetAccessorDeclaration;

            accessors.Add(
                AccessorDeclaration(
                    setKind,
                    List<AttributeListSyntax>(),
                    TokenList(),
                    Token( setKind == SyntaxKind.InitAccessorDeclaration ? SyntaxKind.InitKeyword : SyntaxKind.SetKeyword ),
                    null,
                    null,
                    Token( SyntaxKind.SemicolonToken ) ) );
        }

        var syntheticPropertyDeclaration = PropertyDeclaration(
            List<AttributeListSyntax>(),
            modifiers,
            propertyType,
            null,
            SafeIdentifier( symbol.Name ),
            AccessorList( List( accessors ) ),
            null,
            null );

        if ( this._rewritingDriver.AnalysisRegistry.IsInlined( lastOverride.ToSemantic( IntermediateSymbolSemanticKind.Default ) ) )
        {
            members.Add( GetLinkedDeclaration( IntermediateSymbolSemanticKind.Final ) );
        }
        else
        {
            members.Add(
                this.GetTrampolineForSynthesizedProperty(
                    syntheticPropertyDeclaration,
                    lastOverride.ToSemantic( IntermediateSymbolSemanticKind.Default ),
                    generationContext ) );
        }

        if ( this._rewritingDriver.AnalysisRegistry.IsReachable( symbol.ToSemantic( IntermediateSymbolSemanticKind.Default ) )
             && !this._rewritingDriver.AnalysisRegistry.IsInlined( symbol.ToSemantic( IntermediateSymbolSemanticKind.Default ) )
             && this._rewritingDriver.ShouldGenerateSourceMember( symbol ) )
        {
            members.Add(
                this.GetOriginalImplProperty(
                    symbol,
                    propertyType,
                    generationContext ) );
        }

        if ( this._rewritingDriver.AnalysisRegistry.IsReachable( symbol.ToSemantic( IntermediateSymbolSemanticKind.Base ) )
             && !this._rewritingDriver.AnalysisRegistry.IsInlined( symbol.ToSemantic( IntermediateSymbolSemanticKind.Base ) )
             && this._rewritingDriver.ShouldGenerateEmptyMember( symbol ) )
        {
            members.Add(
                this._rewritingDriver.GetEmptyImplProperty(
                    symbol,
                    List<AttributeListSyntax>(),
                    propertyType,
                    generationContext ) );
        }

        return members;

        PropertyDeclarationSyntax GetLinkedDeclaration( IntermediateSymbolSemanticKind semanticKind )
        {
            var transformedAccessors = new List<AccessorDeclarationSyntax>();

            if ( symbol.GetMethod != null )
            {
                var getAccessorDeclaration = syntheticPropertyDeclaration.AccessorList!.Accessors
                    .Single( a => a.IsKind( SyntaxKind.GetAccessorDeclaration ) );

                var linkedBody = this._rewritingDriver.GetSubstitutedBody(
                    symbol.GetMethod.ToSemantic( semanticKind ),
                    new SubstitutionContext(
                        this._rewritingDriver,
                        generationContext,
                        new InliningContextIdentifier( symbol.GetMethod.ToSemantic( semanticKind ) ) ) );

                transformedAccessors.Add(
                    getAccessorDeclaration.PartialUpdate(
                        body: Block(
                                Token( TriviaList( ElasticMarker ), SyntaxKind.OpenBraceToken, TriviaList( ElasticMarker ) ),
                                SingletonList<StatementSyntax>( linkedBody ),
                                Token( TriviaList( ElasticMarker ), SyntaxKind.CloseBraceToken, TriviaList( ElasticMarker ) ) )
                            .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock )
                            .WithGeneratedCodeAnnotation( FormattingAnnotations.SystemGeneratedCodeAnnotation ),
                        semicolonToken: default(SyntaxToken) ) );
            }

            if ( symbol.SetMethod != null )
            {
                var setAccessorDeclaration = syntheticPropertyDeclaration.AccessorList!.Accessors
                    .Single( a => a.IsKind( SyntaxKind.SetAccessorDeclaration ) || a.IsKind( SyntaxKind.InitAccessorDeclaration ) );

                var linkedBody = this._rewritingDriver.GetSubstitutedBody(
                    symbol.SetMethod.ToSemantic( semanticKind ),
                    new SubstitutionContext(
                        this._rewritingDriver,
                        generationContext,
                        new InliningContextIdentifier( symbol.SetMethod.ToSemantic( semanticKind ) ) ) );

                transformedAccessors.Add(
                    setAccessorDeclaration.PartialUpdate(
                        body: Block(
                                Token( TriviaList( ElasticMarker ), SyntaxKind.OpenBraceToken, TriviaList( ElasticMarker ) ),
                                SingletonList<StatementSyntax>( linkedBody ),
                                Token( TriviaList( ElasticMarker ), SyntaxKind.CloseBraceToken, TriviaList( ElasticMarker ) ) )
                            .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock )
                            .WithGeneratedCodeAnnotation( FormattingAnnotations.SystemGeneratedCodeAnnotation ),
                        semicolonToken: default(SyntaxToken) ) );
            }

            return syntheticPropertyDeclaration
                .PartialUpdate(
                    accessorList: AccessorList(
                            Token( TriviaList( ElasticMarker ), SyntaxKind.OpenBraceToken, TriviaList( ElasticMarker ) ),
                            List( transformedAccessors ),
                            Token( TriviaList( ElasticMarker ), SyntaxKind.CloseBraceToken, TriviaList( ElasticMarker ) ) )
                        .WithGeneratedCodeAnnotation( FormattingAnnotations.SystemGeneratedCodeAnnotation ) )
                .WithOptionalLeadingAndTrailingLineFeed( generationContext )
                .WithGeneratedCodeAnnotation( FormattingAnnotations.SystemGeneratedCodeAnnotation );
        }
    }

    private PropertyDeclarationSyntax GetTrampolineForSynthesizedProperty(
        PropertyDeclarationSyntax property,
        IntermediateSymbolSemantic<IPropertySymbol> targetSymbol,
        SyntaxGenerationContext context )
    {
        var getAccessor = property.AccessorList?.Accessors.SingleOrDefault( x => x.Kind() == SyntaxKind.GetAccessorDeclaration );

        var setAccessor = property.AccessorList?.Accessors.SingleOrDefault(
            x => x.Kind() == SyntaxKind.SetAccessorDeclaration || x.Kind() == SyntaxKind.InitAccessorDeclaration );

        return property
            .PartialUpdate(
                accessorList: AccessorList(
                    List(
                        new[]
                            {
                                getAccessor != null
                                    ? AccessorDeclaration(
                                        SyntaxKind.GetAccessorDeclaration,
                                        context.SyntaxGenerator.FormattedBlock(
                                            ReturnStatement(
                                                TokenWithTrailingSpace( SyntaxKind.ReturnKeyword ),
                                                GetInvocationTarget(),
                                                Token( SyntaxKind.SemicolonToken ) ) ) )
                                    : null,
                                setAccessor != null
                                    ? AccessorDeclaration(
                                        setAccessor.Kind(),
                                        context.SyntaxGenerator.FormattedBlock(
                                            ExpressionStatement(
                                                AssignmentExpression(
                                                    SyntaxKind.SimpleAssignmentExpression,
                                                    GetInvocationTarget(),
                                                    IdentifierName( "value" ) ) ) ) )
                                    : null
                            }.Where( a => a != null )
                            .AssertNoneNull() ) ) )
            .WithTriviaFromIfNecessary( property, this._rewritingDriver.SyntaxGenerationOptions );

        ExpressionSyntax GetInvocationTarget()
        {
            if ( targetSymbol.Symbol.IsStatic )
            {
                return SafeIdentifierName( targetSymbol.Symbol.Name );
            }
            else
            {
                return MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        ThisExpression(),
                        SafeIdentifierName( targetSymbol.Symbol.Name ) )
                    .WithSimplifierAnnotationIfNecessary( context );
            }
        }
    }

    /// <summary>
    /// Creates the source impl property for a synthesized override target, which is what <c>meta.Proceed()</c> reads when the
    /// original implementation is not inlined. Its getter has the body that the compiler would have synthesized.
    /// </summary>
    private MemberDeclarationSyntax GetOriginalImplProperty(
        IPropertySymbol symbol,
        TypeSyntax type,
        SyntaxGenerationContext context )
    {
        var accessors = new List<AccessorDeclarationSyntax>();

        if ( symbol.GetMethod != null )
        {
            var getterBody = SynthesizedRecordMemberBodyGenerator.GetMemberKind( symbol.GetMethod ) != SynthesizedRecordMemberKind.None
                ? this.GetMaterializedBody( symbol.GetMethod, context )
                : GetNotSupportedBody( context );

            accessors.Add(
                AccessorDeclaration(
                    SyntaxKind.GetAccessorDeclaration,
                    getterBody ) );
        }

        if ( symbol.SetMethod != null )
        {
            var setKind = symbol.SetMethod.IsInitOnly
                ? SyntaxKind.InitAccessorDeclaration
                : SyntaxKind.SetAccessorDeclaration;

            // No compiler-synthesized record property has a setter, so there is no body to reproduce for one.
            accessors.Add(
                AccessorDeclaration(
                    setKind,
                    GetNotSupportedBody( context ) ) );
        }

        return this._rewritingDriver.GetSpecialImplProperty(
            List<AttributeListSyntax>(),
            type,
            AccessorList( List( accessors ) ),
            null,
            null,
            symbol,
            LinkerRewritingDriver.GetOriginalImplMemberName( symbol ),
            context );
    }
}