// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Engine.CodeModel.References;

/// <summary>
/// The base implementation of <see cref="IDurableRef{T}"/>, that is, of a reference that may be stored in an object
/// that outlives the run that produced it.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Id"/> is abstract, because the derived classes obtain it differently.
/// <see cref="DeclarationIdRef{T}"/> and <see cref="TypeIdRef{T}"/> store the identifier and hold no reference to a
/// compilation. <see cref="BoundDurableRef{T}"/> stores the <see cref="IFullRef{T}"/> it was created from, and computes
/// the identifier from it when the identifier is requested.
/// </para>
/// <para>
/// Equality and hash codes are computed from <see cref="Id"/> in all derived classes, so that two durable references
/// to the same declaration are equal even when they are of different derived classes.
/// </para>
/// </remarks>
internal abstract class DurableRef<T> : BaseRef<T>, IDurableRef<T>, IDurableRefImpl
    where T : class, ICompilationElement
{
    /// <summary>
    /// A weak reference to the <see cref="IFullRef"/> that the last resolution returned, or <c>null</c> when no
    /// resolution has been cached.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This field is a cache. No other member reads it to produce a value that must always be available. In
    /// particular, <see cref="Id"/> does not: a derived class that computes the identifier computes it from a field
    /// that it holds with a strong reference. When the garbage collector clears this field, the next resolution
    /// resolves the identifier again.
    /// </para>
    /// <para>
    /// The cached reference is also reachable from the <see cref="RefFactory"/> of its compilation, so it remains
    /// alive while that compilation is alive. The weak reference breaks the cycle that would otherwise exist between
    /// this object and the full reference, which returns this object from <see cref="IRef.ToDurable"/>.
    /// </para>
    /// <para>
    /// This field is a <see cref="WeakReference{T}"/> and not a <c>WeakCache</c>. <c>ObjectGraphWalker</c> does not
    /// follow a <see cref="WeakReference{T}"/>, so the memory leak tests do not report the cached reference. That is
    /// the intended behavior, because the cached reference does not extend the lifetime of the compilation.
    /// </para>
    /// </remarks>
    private WeakReference<IFullRef>? _resolvedRefCache;

    /// <summary>
    /// Gets the identifier of the target of this reference.
    /// </summary>
    public abstract string Id { get; }

    /// <summary>
    /// Gets a value indicating whether this reference holds a reference to a compilation. Only
    /// <see cref="BoundDurableRef{T}"/> returns <c>true</c>.
    /// </summary>
    public virtual bool ReachesCompilation => false;

    /// <summary>
    /// Gets a value indicating whether the resolution cache currently holds a reference.
    /// </summary>
    /// <remarks>
    /// This property is used by the unit tests. The cache is required to return the same result as a resolution that
    /// does not use it, so a test that compares results cannot determine whether the cache was used.
    /// </remarks>
    internal bool IsResolutionCached => this._resolvedRefCache is { } cache && cache.TryGetTarget( out _ );

    public abstract IFullRef ToFullRef( RefFactory refFactory );

    public override IDurableRef<T> ToDurable() => this;

    public override bool IsDurable => true;

    /// <summary>
    /// Returns the cached reference when it belongs to <paramref name="refFactory"/> and the resolution cache is
    /// enabled for the project, and <c>null</c> otherwise.
    /// </summary>
    /// <remarks>
    /// The cached reference is accepted only when its <see cref="RefFactory"/> is the same instance. A
    /// <see cref="RefFactory"/> is shared by all versions of one compilation model and by no other compilation, so
    /// comparing the instances establishes that the cached reference resolves in the requested compilation.
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
    /// Stores the reference produced by a resolution, so that a later resolution against the same compilation does not
    /// resolve the identifier again.
    /// </summary>
    private protected void SetCachedRef( RefFactory refFactory, ICompilationElement resolved )
    {
        if ( !refFactory.DurableRefFactory.IsResolutionCacheEnabled )
        {
            return;
        }

        // A reference to an introduced declaration is not cached. The identity of an introduced declaration is not
        // final while the pipeline runs, so a reference obtained now may designate a different declaration later. The
        // identifier is the only representation that resolves in every version of the compilation.
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
