// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Engine.CodeModel.GenericContexts;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Metalama.Framework.Engine.CodeModel.References;

/// <summary>
/// An implementation of <see cref="IRef"/> based on <see cref="IDeclarationBuilder"/>.
/// </summary>
internal sealed partial class IntroducedRef<T> : FullRef<T>, IIntroducedRef
    where T : class, IDeclaration
{
    private readonly GenericContext _genericContext; // Gives the type arguments for the builder.

    /// <summary>
    /// The nullability of the referenced type, which is meaningful for a named type alone and is <c>false</c> for every
    /// other kind of declaration.
    /// </summary>
    /// <remarks>
    /// The annotation is part of the type and not of the declaration that the builder describes, so it cannot be read
    /// from the builder and has to be carried here, as the generic context is. A reference that did not carry it
    /// resolved the nullable form of an introduced type to the non-nullable one, so an aspect that introduced a member
    /// of that type emitted it as non-nullable: the type of a member is carried as a reference from the advice that
    /// builds it to the code that emits it. See issue #1840.
    /// </remarks>
    private readonly bool? _isNullable;

    // We use a StrongBox because:
    // (1) the DeclarationBuilderData may be assigned after the constructor is called, typically just after DeclarationBuilde.Freeze.
    // (2) in the meantime, a copy of this reference may have been taken with the WithGenericContext method.
    private readonly StrongBox<DeclarationBuilderData> _builderData;

    public DeclarationBuilderData BuilderData
    {
        get => this._builderData.Value ?? throw new InvalidOperationException( "The BuilderData property has not been set." );

        set
        {
            Invariant.Assert( this._builderData.Value == null );
            CheckBuilderData( value );
            this._builderData.Value = value;
        }
    }

    public IFullRef? ReplacedDeclaration
        => this.BuilderData switch
        {
            ConstructorBuilderData { ReplacedImplicitConstructor: { } replacedImplicitConstructor } => replacedImplicitConstructor,
            PropertyBuilderData { OriginalField: { } originalField } => originalField,
            _ => null
        };

    /// <summary>
    /// Initializes a new instance of the <see cref="IntroducedRef{TInterface}"/> class when the <see cref="DeclarationBuilderData"/>
    /// is already known.
    /// </summary>
    public IntroducedRef(
        DeclarationBuilderData builderData,
        RefFactory refFactory,
        GenericContext? genericContext = null,
        bool? isNullable = false ) : base( refFactory )
    {
        CheckBuilderData( builderData );
        this._builderData = new StrongBox<DeclarationBuilderData>( builderData );
        this._genericContext = genericContext ?? GenericContext.Empty;
        this._isNullable = isNullable;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IntroducedRef{TInterface}"/> class when the <see cref="DeclarationBuilderData"/>
    /// has not been created yet.
    /// </summary>
    /// <param name="refFactory"></param>
    public IntroducedRef( RefFactory refFactory ) : base( refFactory )
    {
        this._builderData = new StrongBox<DeclarationBuilderData>();
        this._genericContext = GenericContext.Empty;
        this._isNullable = false;
    }

    private IntroducedRef( IntroducedRef<T> prototype, GenericContext? genericContext ) : base( prototype.RefFactory )
    {
        this._builderData = prototype._builderData;
        this._genericContext = genericContext ?? GenericContext.Empty;
        this._isNullable = prototype._isNullable;
    }

    [Conditional( "DEBUG" )]
    private static void CheckBuilderData( DeclarationBuilderData builderData )
    {
#if DEBUG

        // Type parameter must match the builder type.
        Invariant.Assert(
            builderData.DeclarationKind.GetPossibleDeclarationInterfaceTypes().Contains( typeof(T) ),
            $"The interface type was expected to be of type {string.Join( " or", builderData.DeclarationKind.GetPossibleDeclarationInterfaceTypes().SelectAsReadOnlyCollection( t => t.Name ) )} but was {typeof(T)}." );
#endif

        // Constructor replacements must be resolved upstream, but this invariant can no longer be enforced here because the reference
        // is built when the BuilderData is being built.

        // References to promoted fields must be a SymbolRef to the IFieldSymbol if it is an IRef<IField>.
        Invariant.Assert( !(typeof(T) == typeof(IField) && builderData is PropertyBuilderData) );
    }

    public override bool IsDefinition => this._genericContext.IsEmptyOrIdentity;

    public override IFullRef<T> DefinitionRef => this.IsDefinition ? this : (IFullRef<T>) this.BuilderData.ToFullRef();

    public override FullRef<T> WithGenericContext( GenericContext genericContext )
        => genericContext.IsEmptyOrIdentity ? this : new IntroducedRef<T>( this, genericContext );

    public override IFullRef ContainingDeclaration => this.BuilderData.ContainingDeclaration;

    public override IFullRef<INamedType> DeclaringType => this.BuilderData.DeclaringType.AssertNotNull();

    public override string? Name
        => this.BuilderData switch
        {
            NamedDeclarationBuilderData named => named.Name,
            _ => null
        };

    /// <remarks>
    /// The nullable annotation is appended to the identifier, because it is part of the type and not of the declaration
    /// that the documentation identifier names, and a durable reference is identified by its identifier alone. See
    /// issue #1840.
    /// </remarks>
    public override SerializableDeclarationId ToSerializableId()
        => this.ConstructedDeclaration.ToSerializableId().WithNullability( this._isNullable );

    protected override ISymbol GetSymbolIgnoringRefKind( CompilationContext compilationContext ) => throw new NotSupportedException();

    /// <summary>
    /// Returns <c>null</c>, so that an introduced declaration is always identified by its declaration identifier.
    /// </summary>
    /// <remarks>
    /// The base implementation inspects the symbol to detect a constructed generic type, and an introduced declaration
    /// has no symbol. An introduced declaration is never a constructed generic type either, so the declaration
    /// identifier is the correct choice here. It carries the nullable annotation, which <see cref="ToSerializableId"/>
    /// appends, so nothing is lost by identifying the durable reference by that string alone.
    /// </remarks>
    public override SerializableTypeId? ToSerializableTypeId() => null;

    public override ISymbol GetClosestContainingSymbol()
    {
        for ( var ancestor = (IFullRef) this.BuilderData.ContainingDeclaration; ancestor != null; ancestor = ancestor.ContainingDeclaration )
        {
            if ( ancestor is ISymbolRef symbolBasedDeclaration )
            {
                return symbolBasedDeclaration.Symbol;
            }
        }

        // We should always have an containing symbol.
        throw new AssertionFailedException();
    }

    public override SyntaxTree? PrimarySyntaxTree => this.BuilderData.PrimarySyntaxTree;

    private GenericContext SelectGenericContext( IGenericContext genericContext )
    {
        if ( this._genericContext.IsEmptyOrIdentity )
        {
            return (GenericContext) genericContext;
        }
        else if ( genericContext is { IsEmptyOrIdentity: true } )
        {
            return this._genericContext;
        }
        else
        {
            // Both contexts are non-empty. Combine them by mapping this ref's generic context
            // through the passed context. E.g., if this ref has {T->U} and the passed context
            // is {U->int}, the result is {T->int}.
            return this._genericContext.Map( (GenericContext) genericContext, this.RefFactory );
        }
    }

    protected override ICompilationElement? Resolve(
        CompilationModel compilation,
        bool throwIfMissing,
        IGenericContext genericContext,
        Type interfaceType )
        => ConvertDeclarationOrThrow(
            compilation.Factory.GetDeclaration( this.BuilderData, this.SelectGenericContext( genericContext ), interfaceType, this._isNullable ),
            compilation,
            interfaceType );

    public override string ToString() => this.BuilderData.ToString().AssertNotNull();

    protected override IFullRef<TOut> CastAsFullRef<TOut>()
    {
        if ( this is IFullRef<TOut> desired )
        {
            return desired;
        }
        else if ( this.BuilderData.DeclarationKind == DeclarationKind.Property && typeof(TOut) == typeof(IField) )
        {
            var redirectedField = ((PropertyBuilderData) this.BuilderData).OriginalField;

            if ( redirectedField != null )
            {
                return (IFullRef<TOut>) redirectedField.WithGenericContext( this._genericContext );
            }
        }
        else if ( this.BuilderData.DeclarationKind == DeclarationKind.Field && typeof(TOut) == typeof(IProperty) )
        {
            var overridingProperty = ((FieldBuilderData) this.BuilderData).OverridingProperty;

            if ( overridingProperty != null )
            {
                return (IFullRef<TOut>) overridingProperty.WithGenericContext( this._genericContext );
            }
        }

        throw new InvalidCastException( $"Cannot convert the IRef<{typeof(T).Name}> to IRef<{typeof(TOut).Name}>) for '{this}'." );
    }

    public override bool Equals( IRef? other, RefComparison comparison )
    {
        // NOTE: By convention, we want references to be considered different if they resolve to different targets. Therefore, for promoted fields,
        // an IRef<IField> or an IRef<IProperty> to the same PromotedField will be considered different.
        // Since all references are canonical, we only need to support comparison of references of the same type.
        // A reference of any other type is not equal.

        if ( other is not IntroducedRef<T> otherRef )
        {
            return false;
        }

        Invariant.Assert(
            this.CompilationContext == otherRef.CompilationContext ||
            comparison is RefComparison.Structural or RefComparison.StructuralIncludeNullability,
            "Compilation mistmatch in a non-structural comparison." );

        if ( !this.BuilderData.Equals( otherRef.BuilderData ) )
        {
            return false;
        }

        if ( !this._genericContext.Equals( otherRef._genericContext ) )
        {
            return false;
        }

        if ( comparison is RefComparison.IncludeNullability or RefComparison.StructuralIncludeNullability
             && this._isNullable != otherRef._isNullable )
        {
            return false;
        }

        return true;
    }

    // The nullability is deliberately left out of the hash code, so that the same hash serves the comparisons that
    // take it into account and those that do not. Two references differing only by it collide, which is what
    // SymbolEqualityComparer does as well.
    public override int GetHashCode( RefComparison comparison ) => HashCode.Combine( this.BuilderData.GetHashCode(), this._genericContext );

    public override DeclarationKind DeclarationKind => this.BuilderData.DeclarationKind;
}