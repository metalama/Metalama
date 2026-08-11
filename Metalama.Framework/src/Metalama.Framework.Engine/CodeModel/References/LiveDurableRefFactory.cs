// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Engine.CodeModel.References;

/// <summary>
/// The implementation of <see cref="IDurableRefFactory"/> used during a batch compilation, which builds a
/// <see cref="LiveDurableRef{T}"/> rather than an identifier-based reference.
/// </summary>
internal sealed class LiveDurableRefFactory : IDurableRefFactory
{
    public static LiveDurableRefFactory Instance { get; } = new();

    private LiveDurableRefFactory() { }

    /// <summary>
    /// Gets <c>true</c>, because a project that builds live references still reads identifier-based ones, in particular
    /// from the transitive manifest of a referenced project, and those resolve faster with the cache.
    /// </summary>
    public bool IsResolutionCacheEnabled => true;

    public IDurableRef<T> FromFullRef<T>( IFullRef<T> fullRef )
        where T : class, ICompilationElement
        => new LiveDurableRef<T>( fullRef );

    /// <inheritdoc cref="SerializableDurableRefFactory.FromDeclarationOrType{T}"/>
    /// <remarks>
    /// The type case does not need to precede the declaration case as it does in
    /// <see cref="SerializableDurableRefFactory"/>, because holding the reference loses neither the type arguments nor
    /// the nullable annotation. Both cases are kept nonetheless, so that the two factories accept and reject exactly
    /// the same arguments.
    /// </remarks>
    public IDurableRef<T> FromDeclarationOrType<T>( ICompilationElement declarationOrType )
        where T : class, ICompilationElement
    {
        var reference = declarationOrType switch
        {
            IAttribute => throw new NotSupportedException( AttributeRef.CannotBeMadeDurableMessage ),
            IType type => type.ToRef().As<T>(),
            IDeclaration declaration => declaration.ToRef().As<T>(),
            _ => throw new NotSupportedException(
                $"Cannot create a durable reference to a '{declarationOrType.DeclarationKind}' because it is neither a declaration nor a type." )
        };

        return new LiveDurableRef<T>( (IFullRef<T>) reference );
    }
}
