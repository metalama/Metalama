// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.GenericContexts;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace Metalama.Framework.Engine.CodeModel.References;

internal sealed partial class SyntaxRef<T> : FullRef<T>
    where T : class, ICompilationElement
{
    private readonly SyntaxNode _syntaxNode;

    public SyntaxRef( SyntaxNode syntaxNode, RefTargetKind targetKind, RefFactory refFactory ) : base( refFactory )
    {
        this._syntaxNode = syntaxNode.AssertNotNull();
        this.TargetKind = targetKind;
    }

    public override FullRef<T> WithGenericContext( GenericContext genericContext ) => throw new NotImplementedException();

    public override bool IsDefinition => true;

    public override IFullRef<T> DefinitionRef => this;

    public override RefTargetKind TargetKind { get; }

    public override IFullRef ContainingDeclaration => throw new NotImplementedException();

    public override IFullRef<INamedType> DeclaringType => throw new NotImplementedException();

    protected override ISymbol GetSymbolIgnoringRefKind( CompilationContext compilationContext )
    {
        Invariant.Assert( this.CompilationContext == compilationContext );

        return this.Symbol;
    }

    public override SyntaxTree PrimarySyntaxTree => this._syntaxNode.SyntaxTree;

    [Memo]
    private ISymbol Symbol => this.GetSymbol();

    private ISymbol GetSymbol()
    {
        var semanticModel =
            this.CompilationContext.SemanticModelProvider.GetSemanticModel( this._syntaxNode.SyntaxTree )
            ?? throw new AssertionFailedException( $"Cannot get a semantic model for '{this._syntaxNode.SyntaxTree.FilePath}'." );

        return (this._syntaxNode is LambdaExpressionSyntax
                   ? semanticModel.GetSymbolInfo( this._syntaxNode ).Symbol
                   : semanticModel.GetDeclaredSymbol( this._syntaxNode ))
               ?? throw new AssertionFailedException( $"Cannot get a symbol for {this._syntaxNode.GetType().Name}." );
    }

    protected override ICompilationElement? Resolve(
        CompilationModel compilation,
        bool throwIfMissing,
        IGenericContext genericContext,
        Type interfaceType )
    {
        return ConvertDeclarationOrThrow(
            compilation.Factory.GetCompilationElement(
                    this.Symbol,
                    this.TargetKind,
                    genericContext )
                .AssertNotNull(),
            compilation,
            interfaceType );
    }

    public override string ToString()
        => this.TargetKind switch
        {
            RefTargetKind.Default => this._syntaxNode.GetType().Name,
            _ => $"{this._syntaxNode.GetType().Name}:{this.TargetKind}"
        };

    protected override IFullRef<TOut> CastAsFullRef<TOut>()
        => this as IFullRef<TOut> ?? new SyntaxRef<TOut>( this._syntaxNode, this.TargetKind, this.RefFactory );

    public override bool Equals( IRef? other, RefComparison comparison )
    {
        if ( comparison != RefComparison.Default )
        {
            throw new ArgumentOutOfRangeException( nameof(comparison), "Only RefComparison.Default is supported for SyntaxRef." );
        }

        if ( other is null )
        {
            return false;
        }

        // The whole point of SyntaxRef is to avoid resolving the semantic model until and if necessary.
        // Therefore, by design, we don't resolve to symbols before comparing, which means that we cannot
        // compare to other kind of references.
        if ( other is not SyntaxRef<T> syntaxRef )
        {
            throw new NotSupportedException( "A SyntaxRef can only be compared to another SyntaxRef." );
        }

        return this._syntaxNode == syntaxRef._syntaxNode && this.TargetKind == syntaxRef.TargetKind;
    }

    public override int GetHashCode( RefComparison comparison )
    {
        if ( comparison != RefComparison.Default )
        {
            throw new ArgumentOutOfRangeException( nameof(comparison), "Only RefComparison.Default is supported for SyntaxRef." );
        }

        return HashCode.Combine( this._syntaxNode, this.TargetKind );
    }

    public override DeclarationKind DeclarationKind => throw new NotSupportedException();
}