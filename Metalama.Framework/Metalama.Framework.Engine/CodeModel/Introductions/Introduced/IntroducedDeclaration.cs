// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Collections;
using Metalama.Framework.Engine.CodeModel.GenericContexts;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.CodeModel.Visitors;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using SyntaxReference = Microsoft.CodeAnalysis.SyntaxReference;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Introduced;

/// <summary>
/// The base class for the read-only facade of introduced declarations, represented by <see cref="DeclarationBuilder"/>. Facades
/// are consistent with the consuming <see cref="CompilationModel"/>, while builders are consistent with the producing <see cref="CompilationModel"/>. 
/// </summary>
internal abstract class IntroducedDeclaration : BaseDeclaration
{
    protected IntroducedDeclaration( CompilationModel compilation, IGenericContext genericContext )
    {
        this.Compilation = compilation;
        this.GenericContext = genericContext.AsGenericContext();
    }

    public override CompilationModel Compilation { get; }

    public abstract DeclarationBuilderData BuilderData { get; }

    internal override GenericContext GenericContext { get; }

    public sealed override string ToDisplayString( CodeDisplayFormat? format = null, CodeDisplayContext? context = null )
        => DisplayStringFormatter.Format( this, format, context, this.GenericContext );

    public override IAssembly DeclaringAssembly => this.Compilation;

    public override IDeclarationOrigin Origin => this.BuilderData.ParentAdvice.AspectInstance;

    [return: NotNullIfNotNull( nameof(type) )]
    protected IType? MapType( IRef<IType>? type )
    {
        if ( type == null )
        {
            return null;
        }

        return (IType) type.GetTarget( this.Compilation, this.GenericContext, typeof(IType) );
    }

    [return: NotNullIfNotNull( nameof(declaration) )]
    protected T? MapDeclaration<T>( IRef<T>? declaration )
        where T : class, ICompilationElement
        => declaration?.GetTarget( this.Compilation, this.GenericContext );

#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
    protected ImmutableArray<T> MapDeclarationList<T>( IReadOnlyList<IRef<T>> refs )
        where T : class, ICompilationElement
        => refs.Count == 0 ? ImmutableArray<T>.Empty : refs.SelectAsImmutableArray( this.MapDeclaration );
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.

    [Memo]
    public override IDeclaration ContainingDeclaration => this.MapDeclaration( this.BuilderData.ContainingDeclaration );

    public sealed override SyntaxTree? PrimarySyntaxTree => this.BuilderData.PrimarySyntaxTree;

    [Memo]
    public override IAttributeCollection Attributes => this.GetAttributes();

    private IAttributeCollection GetAttributes()
    {
        // In the Attributes collection, the backlink IAttribute.ContainingDeclaration will point to the member definition but this should be ok.

        var definition = this.GetDefinition();

        return new AttributeCollection(
            definition,
            this.Compilation.GetAttributeCollection( definition.ToFullRef() ) );
    }

    public override DeclarationKind DeclarationKind => this.BuilderData.DeclarationKind;

    public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => ImmutableArray<SyntaxReference>.Empty;

    public override Location? DiagnosticLocation => this.ContainingDeclaration.GetDiagnosticLocation();

    public override bool IsImplicitlyDeclared => false;

    public override bool Equals( IDeclaration? other )
    {
        switch ( other )
        {
            case IntroducedDeclaration builtDeclaration:
                return this.BuilderData.Equals( builtDeclaration.BuilderData ) && this.GenericContext.Equals( builtDeclaration.GenericContext );

            default:
                return false;
        }
    }

    protected override int GetHashCodeCore() => HashCode.Combine( this.BuilderData.GetHashCode(), this.GenericContext );

    public override bool BelongsToCurrentProject => true;

    public override ImmutableArray<SourceReference> Sources => ImmutableArray<SourceReference>.Empty;

    internal override ICompilationElement Translate(
        CompilationModel newCompilation,
        IGenericContext? genericContext = null )
    {
        GenericContext combinedGenericContext;

        if ( genericContext is { IsEmptyOrIdentity: false } )
        {
            if ( this.GenericContext.IsEmptyOrIdentity )
            {
                combinedGenericContext = (GenericContext) genericContext;
            }
            else
            {
                throw new AssertionFailedException( "Don't know how to combine generic contexts." );
            }
        }
        else
        {
            combinedGenericContext = this.GenericContext;
        }

        return this.BuilderData.ToFullRef().GetTarget( newCompilation, combinedGenericContext );
    }

    internal sealed override DeclarationImplementationKind ImplementationKind => DeclarationImplementationKind.Introduced;
}