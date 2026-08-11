// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using System;

namespace Metalama.Framework.Engine.CodeModel.References;

/// <summary>
/// The implementation of <see cref="IDurableRef{T}"/> used during a batch compilation, which holds the
/// <see cref="FullRef{T}"/> it was made from instead of collapsing it to an identifier.
/// </summary>
/// <remarks>
/// <para>
/// A durable reference exists so that an object outliving a single request does not keep a compilation in memory. A
/// batch compilation has one compilation, which outlives every object the run produces, so this reference is durable in
/// the only sense that matters there, and it costs neither the identifier that
/// <see cref="SerializableDurableRefFactory"/> computes when the reference is made, nor the symbol table lookup that
/// resolving that identifier costs afterwards. See issue #1811.
/// </para>
/// <para>
/// The underlying reference is held strongly and is never null, which is what makes <see cref="Id"/> available at any
/// time, including long after every other route to the compilation is gone. Nothing here reads the resolution cache of
/// the base class.
/// </para>
/// </remarks>
internal sealed class LiveDurableRef<T> : DurableRef<T>
    where T : class, ICompilationElement
{
    private readonly IFullRef<T> _underlying;
    private IDurableRefImpl? _serializableRef;

    public LiveDurableRef( IFullRef<T> underlying )
    {
        this._underlying = underlying;
    }

    /// <summary>
    /// Gets the identifier-based reference that this reference would have been had the project used
    /// <see cref="SerializableDurableRefFactory"/>.
    /// </summary>
    /// <remarks>
    /// Deriving the identifier from that reference rather than computing it here makes the two kinds agree by
    /// construction rather than by resemblance. The distinction is not cosmetic: a named type is a declaration, so
    /// asking <see cref="FullRef{T}.ToSerializableId"/> directly would answer with a declaration identifier and lose
    /// the type arguments and the nullable annotation, which is the defect reported as issue #1797.
    /// </remarks>
    private IDurableRefImpl SerializableRef
        => this._serializableRef ??= (IDurableRefImpl) SerializableDurableRefFactory.Instance.FromFullRef( this._underlying );

    public override string Id => this.SerializableRef.Id;

    public override bool ReachesCompilation => true;

    public override SerializableDeclarationId ToSerializableId() => this.SerializableRef.ToSerializableId();

    protected override ISymbol GetSymbol( CompilationContext compilationContext, bool ignoreAssemblyKey = false )
        => (ReferenceEquals( compilationContext, this._underlying.RefFactory.CompilationContext )
                ? this._underlying.GetSymbol( compilationContext.Compilation, ignoreAssemblyKey )
                : this.SerializableRef.GetSymbol( compilationContext.Compilation, ignoreAssemblyKey ))
            .AssertSymbolNotNull();

    protected override ICompilationElement? Resolve(
        CompilationModel compilation,
        bool throwIfMissing,
        IGenericContext genericContext,
        Type interfaceType )
    {
        Invariant.Assert( genericContext.IsEmptyOrIdentity );

        // The underlying reference answers only for the compilation model lineage it belongs to. A RefFactory is shared
        // by every version of one lineage and by nothing else, so this single reference comparison establishes that.
        // Any other compilation, in particular the one a consuming project builds, goes through the identifier.
        if ( ReferenceEquals( compilation.RefFactory, this._underlying.RefFactory ) )
        {
            return this._underlying.GetTargetInterface( compilation, interfaceType, null, throwIfMissing );
        }

        return this.SerializableRef.GetTargetInterface( compilation, interfaceType, null, throwIfMissing );
    }

    protected override IRef<TOut> CastAsRef<TOut>()
        => this as IRef<TOut> ?? new LiveDurableRef<TOut>( this._underlying.As<TOut>() );

    public override IFullRef ToFullRef( RefFactory refFactory )
        => ReferenceEquals( refFactory, this._underlying.RefFactory )
            ? this._underlying
            : this.SerializableRef.ToFullRef( refFactory );
}
