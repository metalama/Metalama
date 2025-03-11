// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Source;
using Metalama.Framework.Engine.CompileTime.Serialization.Serializers;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.References;

internal sealed class SyntaxAttributeRef : AttributeRef
{
    private readonly IFullRef<INamedType> _attributeType;
    private readonly RefFactory _refFactory;
    private readonly RefTargetKind _targetKind;
    private readonly SyntaxNode _syntaxNode;

    private ResolvedAttributeRef? _resolvedRef;

    public SyntaxAttributeRef(
        IFullRef<INamedType> attributeType,
        AttributeSyntax attributeSyntax,
        SyntaxNode syntaxNode,
        RefFactory refFactory,
        RefTargetKind targetKind = RefTargetKind.Default )
    {
        this._attributeType = attributeType;
        this.AttributeSyntax = attributeSyntax;
        this._syntaxNode = syntaxNode;
        this._refFactory = refFactory;
        this._targetKind = targetKind;
    }

    [Memo]
    public override IRef<IDeclaration> ContainingDeclaration
        => this._targetKind switch
        {
            RefTargetKind.Module or RefTargetKind.Assembly => this._refFactory.ForCompilation(),
            _ => new SyntaxRef<IDeclaration>( this._syntaxNode, this._targetKind, this._refFactory )
        };

    public override IRef<INamedType> AttributeType => this._attributeType;

    private ResolvedAttributeRef? ResolveAttributeData( AttributeSyntax attributeSyntax )
    {
        if ( this._resolvedRef != null )
        {
            if ( this._resolvedRef == ResolvedAttributeRef.Invalid )
            {
                return null;
            }
            else
            {
                return this._resolvedRef;
            }
        }

        // Find the parent declaration.
        var resolved = this.ContainingDeclaration.ToFullRef( this._refFactory ).GetAttributes();

        // In the parent, find the AttributeData corresponding to the current item.

        var attributeData = resolved.Attributes.SingleOrDefault(
            a => a.ApplicationSyntaxReference != null && a.ApplicationSyntaxReference.Span == attributeSyntax.Span
                                                      && a.ApplicationSyntaxReference.SyntaxTree == attributeSyntax.SyntaxTree );

        if ( attributeData != null )
        {
            if ( resolved.Attributes.Length != 1 )
            {
                resolved = resolved with { Attributes = ImmutableArray.Create( attributeData ) };
            }

            // Save the resolved AttributeData.
            return this._resolvedRef = resolved;
        }
        else
        {
            this._resolvedRef = ResolvedAttributeRef.Invalid;

            return null;
        }
    }

    public override bool TryGetTarget( CompilationModel compilation, [NotNullWhen( true )] out IAttribute? attribute )
    {
        var resolved = this.ResolveAttributeData( this.AttributeSyntax );

        if ( resolved == null )
        {
            attribute = null;

            return false;
        }

        attribute = new SourceAttribute(
            resolved.Attributes[0],
            compilation,
            compilation.Factory.GetDeclaration( resolved.ParentSymbol, resolved.ParentRefTargetKind ) );

        return true;
    }

    public override bool TryGetAttributeSerializationDataKey( [NotNullWhen( true )] out object? serializationDataKey )
    {
        serializationDataKey = this.AttributeSyntax;

        return true;
    }

    public override bool TryGetAttributeSerializationData( [NotNullWhen( true )] out AttributeSerializationData? serializationData )
    {
        var resolved = this.ResolveAttributeData( this.AttributeSyntax );

        if ( resolved == null )
        {
            serializationData = null;

            return false;
        }

        serializationData = new AttributeSerializationData( resolved.ParentSymbol, resolved.Attributes[0], this._refFactory );

        return true;
    }

    public override string Name => throw new NotSupportedException();

    protected override AttributeSyntax AttributeSyntax { get; }

    protected override int GetHashCodeCore() => this.AttributeSyntax.GetHashCode();
}