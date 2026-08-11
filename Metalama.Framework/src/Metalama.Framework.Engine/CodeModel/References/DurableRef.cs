// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Engine.CodeModel.References;

/// <summary>
/// The base implementation of <see cref="IDurableRef{T}"/>, that is, of a reference that may be stored in an object
/// outliving the run that produced it.
/// </summary>
/// <remarks>
/// <para>
/// This class stores nothing. Each derived class owns the storage its own <see cref="Id"/> comes from, because the two
/// answers to "where does the identifier come from" have incompatible lifetimes:
/// <see cref="DeclarationIdRef{T}"/> and <see cref="TypeIdRef{T}"/> hold the identifier itself and reach no
/// compilation, whereas <see cref="LiveDurableRef{T}"/> holds the reference it was made from and computes the
/// identifier from it on demand.
/// </para>
/// <para>
/// Equality and hashing are defined on the identifier for every derived class, so that a durable reference compares
/// equal to another durable reference to the same declaration whatever the kind of either.
/// </para>
/// </remarks>
internal abstract class DurableRef<T> : BaseRef<T>, IDurableRef<T>, IDurableRefImpl
    where T : class, ICompilationElement
{
    /// <summary>
    /// The last reference this one resolved to, held weakly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a cache and nothing else. No value that has to be available at any time is derived from it, and in
    /// particular <see cref="Id"/> never is: a derived class whose identifier is computed rather than stored computes
    /// it from a field it holds strongly. Losing this field to a collection therefore costs a symbol table lookup and
    /// nothing more.
    /// </para>
    /// <para>
    /// The cached reference is itself reachable from <c>RefFactory</c>, which the compilation reaches, so it is alive
    /// exactly while the compilation it belongs to is alive, and the cycle from that reference back to this one
    /// through <c>FullRef.ToDurable</c> is broken here. Note that this is a plain <see cref="WeakReference{T}"/> rather
    /// than a <c>WeakCache</c>, which <c>ObjectGraphWalker</c> cannot see through: the memory leak tests therefore do
    /// not report what is cached here, which is the intended answer.
    /// </para>
    /// </remarks>
    private WeakReference<IFullRef>? _resolvedRefCache;

    /// <summary>
    /// Gets the identifier of the target of this reference.
    /// </summary>
    public abstract string Id { get; }

    /// <summary>
    /// Gets a value indicating whether this reference reaches a compilation, which only
    /// <see cref="LiveDurableRef{T}"/> does.
    /// </summary>
    public virtual bool ReachesCompilation => false;

    /// <summary>
    /// Gets a value indicating whether the resolution cache currently holds a reference.
    /// </summary>
    /// <remarks>
    /// This exists for the tests. Whether the cache answered a resolution is not otherwise observable, and must not be:
    /// the cache is required to leave the result of every resolution unchanged, so a test of the results alone cannot
    /// tell an implementation that consults it from one that ignores it.
    /// </remarks>
    internal bool IsResolutionCached => this._resolvedRefCache is { } cache && cache.TryGetTarget( out _ );

    public abstract IFullRef ToFullRef( RefFactory refFactory );

    public override IDurableRef<T> ToDurable() => this;

    public override bool IsDurable => true;

    /// <summary>
    /// Returns the reference this one last resolved to, when it belongs to <paramref name="refFactory"/> and the
    /// resolution cache is enabled for the project, and <c>null</c> otherwise.
    /// </summary>
    /// <remarks>
    /// The candidate is accepted only when it comes from the same <see cref="RefFactory"/> instance, not merely from an
    /// equal compilation context. A <see cref="RefFactory"/> is shared by every version of a compilation model in one
    /// lineage and by nothing else, so this single reference comparison establishes that the cached reference resolves
    /// in the requested compilation.
    /// </remarks>
    private protected IFullRef? GetCachedRef( RefFactory refFactory )
    {
        if ( !refFactory.DurableRefFactory.IsResolutionCacheEnabled )
        {
            return null;
        }

        if ( this._resolvedRefCache is { } cache
             && cache.TryGetTarget( out var cached )
             && ReferenceEquals( cached.RefFactory, refFactory ) )
        {
            return cached;
        }

        return null;
    }

    /// <summary>
    /// Records the reference that a resolution produced, so that a later resolution against the same compilation does
    /// not go through the symbol table again.
    /// </summary>
    private protected void SetCachedRef( RefFactory refFactory, ICompilationElement resolved )
    {
        if ( !refFactory.DurableRefFactory.IsResolutionCacheEnabled )
        {
            return;
        }

        // A reference to an introduced declaration is deliberately not cached. Its identity is not settled while the
        // pipeline runs, so a reference obtained now may not describe the same declaration later, and the identifier
        // is the only representation that resolves in every compilation.
        var resolvedRef = resolved switch
        {
            IDeclaration declaration => declaration.ToRef() as IFullRef,
            IType type => type.ToRef() as IFullRef,
            _ => null
        };

        if ( resolvedRef is null or IIntroducedRef )
        {
            return;
        }

        this._resolvedRefCache = new WeakReference<IFullRef>( resolvedRef );
    }

    public override bool Equals( IRef? other, RefComparison comparison )
    {
        if ( other == null )
        {
            return false;
        }

        if ( other is not IDurableRefImpl stringRef )
        {
            if ( comparison is RefComparison.Structural or RefComparison.StructuralIncludeNullability )
            {
                return this.Equals( other.ToDurable(), comparison );
            }
            else
            {
                return false;
            }
        }

        // String comparisons are always portable and null-sensitive, so we ignore all flags.

        return stringRef.Id == this.Id;
    }

    public override int GetHashCode( RefComparison comparison )
    {
#if NET5_0_OR_GREATER
        return this.Id.GetHashCode( StringComparison.Ordinal );
#else
        return this.Id.GetHashCode();
#endif
    }

    public override string ToString() => this.Id;
}
