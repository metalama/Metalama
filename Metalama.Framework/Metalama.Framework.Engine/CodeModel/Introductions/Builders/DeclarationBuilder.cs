// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.GenericContexts;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.Collections;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.CodeModel.Visitors;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Framework.Metrics;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using SyntaxReference = Microsoft.CodeAnalysis.SyntaxReference;
using TypedConstant = Metalama.Framework.Code.TypedConstant;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Builders;

/// <summary>
/// Base class implementing <see cref="IDeclarationBuilder"/>. These classes are returned by introduction advice methods so the user can continue
/// specifying the introduced declaration. They are bound to the <see cref="CompilationModel"/> that created them.
/// 
/// </summary>
internal abstract class DeclarationBuilder : IDeclarationBuilderImpl
{
    public AttributeBuilderCollection Attributes { get; } = [];

    public abstract bool IsDesignTimeObservable { get; }

    protected DeclarationBuilder( AspectLayerInstance aspectLayerInstance )
    {
        this.AspectLayerInstance = aspectLayerInstance;
    }

    protected T Translate<T>( T compilationElement )
        where T : class, ICompilationElement
        => compilationElement.ForCompilation( this.Compilation );

    protected TypedConstant? Translate( TypedConstant? typedConstant ) => typedConstant?.ForCompilation( this.Compilation );

    public AspectLayerInstance AspectLayerInstance { get; }

    public IDeclarationOrigin Origin => this.AspectLayerInstance.AspectInstance;

    public abstract IDeclaration? ContainingDeclaration { get; }

    IAttributeCollection IDeclaration.Attributes => this.Attributes;

    public abstract DeclarationKind DeclarationKind { get; }

    public virtual bool IsImplicitlyDeclared => false;

    int IDeclaration.Depth => this.GetDepthImpl();

    bool IDeclaration.BelongsToCurrentProject => true;

    ImmutableArray<SourceReference> IDeclaration.Sources => ImmutableArray<SourceReference>.Empty;

    IGenericContext IDeclaration.GenericContext => GenericContext.Empty;

    ICompilation ICompilationElement.Compilation => this.Compilation;

    ICompilationElement ICompilationElementImpl.Translate( CompilationModel newCompilation, IGenericContext? genericContext, Type? interfaceType )
        => this.ToFullDeclarationRef().GetTarget( newCompilation, genericContext, interfaceType );

    public CompilationModel Compilation
        => (CompilationModel?) this.ContainingDeclaration?.Compilation
           ?? throw new AssertionFailedException( $"Declaration '{this}' has no containing declaration." );

    public bool IsFrozen { get; private set; }

    protected void CheckNotFrozen()
    {
        if ( this.IsFrozen )
        {
            throw new InvalidOperationException( $"You can no longer modify '{this.ToDisplayString()}'." );
        }
    }

    public string ToDisplayString( CodeDisplayFormat? format = null, CodeDisplayContext? context = null )
        => DisplayStringFormatter.Format( this, format, context );

    public void AddAttribute( AttributeConstruction attribute )
    {
        this.CheckNotFrozen();

        this.Attributes.Add( new AttributeBuilder( this.AspectLayerInstance, this, attribute ) );
    }

    public void AddAttributes( IEnumerable<AttributeConstruction> attributes )
    {
        this.CheckNotFrozen();

        this.Attributes.AddRange( attributes.Select( a => new AttributeBuilder( this.AspectLayerInstance, this, a ) ) );
    }

    public void RemoveAttributes( INamedType type )
    {
        this.CheckNotFrozen();

        this.Attributes.RemoveAll( a => a.Type.IsConvertibleTo( type ) );
    }

    public virtual void Freeze()
    {
        if ( this.IsFrozen )
        {
            return;
        }
        
        this.IsFrozen = true;

        this.EnsureReferenceCreated();
        this.FreezeChildren();
        this.EnsureReferenceInitialized();
    }

    protected virtual void FreezeChildren()
    {
        foreach ( var attribute in this.Attributes )
        {
            attribute.Freeze();
        }
    }

    protected abstract void EnsureReferenceInitialized();

    protected virtual void EnsureReferenceCreated() { }

    public SerializableDeclarationId ToSerializableId()
    {
        if ( !this.IsFrozen )
        {
            throw new InvalidOperationException( $"Cannot get the SerializableId of {this.DeclarationKind.ToDisplayString()} '{this}' until it is frozen." );
        }

        return this.GetSerializableId();
    }

    public IRef<IDeclaration> ToRef() => this.ToFullDeclarationRef();

    protected abstract IFullRef<IDeclaration> ToFullDeclarationRef();

    public ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        => ((IDeclarationImpl?) this.ContainingDeclaration)?.DeclaringSyntaxReferences ?? ImmutableArray<SyntaxReference>.Empty;

    public abstract bool CanBeInherited { get; }

    public virtual SyntaxTree? PrimarySyntaxTree => this.ContainingDeclaration.AssertNotNull().GetPrimarySyntaxTree();

    public IEnumerable<IDeclaration> GetDerivedDeclarations( DerivedTypesOptions options = default ) => throw new NotImplementedException();

    DeclarationImplementationKind IDeclarationImpl.ImplementationKind => DeclarationImplementationKind.Builder;

    public sealed override string ToString() => this.ToDisplayString( CodeDisplayFormat.ShortDiagnosticMessage );

    public IAssembly DeclaringAssembly => this.Compilation.DeclaringAssembly;

    // TODO: should we locate diagnostic on the aspect attribute?
    public Location? DiagnosticLocation => null;

    public TExtension GetMetric<TExtension>()
        where TExtension : IMetric
        => this.Compilation.MetricManager.GetMetric<TExtension>( this );

    public virtual bool Equals( IDeclaration? other ) => ReferenceEquals( this, other );
}