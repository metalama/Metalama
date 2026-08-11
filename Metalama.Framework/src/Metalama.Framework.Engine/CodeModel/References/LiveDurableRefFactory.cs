// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Engine.CodeModel.References;

/// <summary>
/// The implementation of <see cref="IDurableRefFactory"/> used during a batch compilation. It creates a
/// <see cref="LiveDurableRef{T}"/> instead of an identifier-based reference.
/// </summary>
internal sealed class LiveDurableRefFactory : IDurableRefFactory
{
    public static LiveDurableRefFactory Instance { get; } = new();

    private LiveDurableRefFactory() { }

    /// <summary>
    /// Gets <c>true</c>. A project that creates live references also deserializes identifier-based references, in
    /// particular from the transitive manifest of a referenced project, and those references benefit from the cache.
    /// </summary>
    public bool IsResolutionCacheEnabled => true;

    public IDurableRef<T> FromFullRef<T>( IFullRef<T> fullRef )
        where T : class, ICompilationElement
        => new LiveDurableRef<T>( fullRef );

    /// <inheritdoc cref="SerializableDurableRefFactory.FromDeclarationOrType{T}"/>
    /// <remarks>
    /// In this implementation, the order of the <see cref="IType"/> and <see cref="IDeclaration"/> cases has no effect,
    /// because storing the reference preserves the type arguments and the nullable annotation. Both cases are present
    /// so that the two implementations accept and reject the same arguments.
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
