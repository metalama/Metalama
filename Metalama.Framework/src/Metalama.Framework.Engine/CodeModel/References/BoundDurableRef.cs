// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using System;

namespace Metalama.Framework.Engine.CodeModel.References;

/// <summary>
/// The implementation of <see cref="IDurableRef{T}"/> used during a batch compilation. It stores the
/// <see cref="IFullRef{T}"/> it was created from instead of an identifier.
/// </summary>
/// <remarks>
/// <para>
/// A batch compilation processes a single compilation, which lives until the build ends. This class therefore avoids
/// two operations: the computation of the identifier when the durable reference is created, and the resolution of that
/// identifier through the symbol table when the reference is resolved. See issue #1811.
/// </para>
/// <para>
/// The field that stores the underlying reference is read-only and never <c>null</c>, so <see cref="Id"/> can always be
/// computed. This class does not use the resolution cache of the base class.
/// </para>
/// </remarks>
internal sealed class BoundDurableRef<T> : DurableRef<T>
    where T : class, ICompilationElement
{
    private readonly IFullRef<T> _underlying;
    private IDurableRefImpl? _serializableRef;

    public BoundDurableRef( IFullRef<T> underlying )
    {
        this._underlying = underlying;
    }

    /// <summary>
    /// Gets the identifier-based reference that <see cref="SerializableDurableRefFactory"/> would have created for the
    /// same declaration.
    /// </summary>
    /// <remarks>
    /// The identifier is obtained from that reference instead of being computed here, so that both representations
    /// produce the same identifier. Computing it here would be error-prone: a named type is also a declaration, so
    /// <see cref="FullRef{T}.ToSerializableId"/> returns a declaration identifier for a named type, and a declaration
    /// identifier does not contain the type arguments or the nullable annotation. That loss is the defect reported as
    /// issue #1797.
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

        // The underlying reference can be resolved only in the compilation model it belongs to. A RefFactory is shared
        // by all versions of a single compilation model and by no other compilation, so comparing the instances is
        // sufficient. Any other compilation, such as the one built by a consuming project, is resolved through the
        // identifier.
        if ( ReferenceEquals( compilation.RefFactory, this._underlying.RefFactory ) )
        {
            return this._underlying.GetTargetInterface( compilation, interfaceType, null, throwIfMissing );
        }

        return this.SerializableRef.GetTargetInterface( compilation, interfaceType, null, throwIfMissing );
    }

    protected override IRef<TOut> CastAsRef<TOut>()
        => this as IRef<TOut> ?? new BoundDurableRef<TOut>( this._underlying.As<TOut>() );

    public override IFullRef ToFullRef( RefFactory refFactory )
        => ReferenceEquals( refFactory, this._underlying.RefFactory )
            ? this._underlying
            : this.SerializableRef.ToFullRef( refFactory );
}
