// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.SerializableIds;
using System;

namespace Metalama.Framework.Engine.CodeModel.References;

/// <summary>
/// The implementation of <see cref="IDurableRefFactory"/> that identifies the target of a durable reference by a
/// <see cref="SerializableDeclarationId"/> or a <see cref="SerializableTypeId"/>, so that the reference reaches no
/// compilation.
/// </summary>
/// <remarks>
/// <para>
/// This is what design time requires, and it is what every scope other than a batch compilation uses.
/// </para>
/// <para>
/// It is also the form in which every durable reference is written and read, whatever the scope that produced it, which
/// is what the name refers to: <see cref="LiveDurableRef{T}"/> asks this factory for the identifier it writes, and a
/// deserialized reference is always one of the two kinds this factory builds.
/// </para>
/// </remarks>
internal sealed class SerializableDurableRefFactory : IDurableRefFactory
{
    /// <summary>
    /// Gets the instance used by the projects that have no <see cref="IDurableRefFactory"/> of their own, and by the
    /// call sites that have no service provider at all, such as the deserialization of a reference.
    /// </summary>
    public static SerializableDurableRefFactory Instance { get; } = new( isResolutionCacheEnabled: true );

    /// <summary>
    /// Gets the instance that resolves every reference through the symbol table, which the test suites use in order to
    /// keep that path covered.
    /// </summary>
    public static SerializableDurableRefFactory InstanceWithoutResolutionCache { get; } = new( isResolutionCacheEnabled: false );

    private SerializableDurableRefFactory( bool isResolutionCacheEnabled )
    {
        this.IsResolutionCacheEnabled = isResolutionCacheEnabled;
    }

    public bool IsResolutionCacheEnabled { get; }

    public IDurableRef<T> FromFullRef<T>( IFullRef<T> fullRef )
        where T : class, ICompilationElement
        => fullRef.GetDurableTypeId() is { } typeId
            ? new TypeIdRef<T>( typeId )
            : new DeclarationIdRef<T>( fullRef.ToSerializableId() );

    /// <summary>
    /// Returns a durable reference to a given declaration or type, computing the identifier from the declaration or
    /// type itself instead of going through the non-durable <see cref="IFullRef{T}"/> that
    /// <see cref="IDeclaration.ToRef"/> returns.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A declaration is identified by its <see cref="SerializableDeclarationId"/>, which is also what
    /// <see cref="IRef.ToDurable"/> uses, so both routes produce an equal reference.
    /// </para>
    /// <para>
    /// A type that is not a declaration, such as an array type or a pointer type, has no declaration identifier and is
    /// therefore identified by its <see cref="SerializableTypeId"/>.
    /// </para>
    /// <para>
    /// A type is identified by its <see cref="SerializableTypeId"/> even when it is also a declaration, which a named
    /// type is, and the type case is therefore matched first. A declaration identifier names a declaration, and the
    /// identity of a type is more than the declaration it comes from: it includes the type arguments, so
    /// <c>ICovariant&lt;Derived&gt;</c> would come back as <c>ICovariant&lt;T&gt;</c>, and it includes the nullable
    /// annotation, so <c>IService?</c> would come back as <c>IService</c>. Neither loss is reported, and the caller has
    /// no reason to think the conversion is lossy. See issue #1797.
    /// </para>
    /// </remarks>
    public IDurableRef<T> FromDeclarationOrType<T>( ICompilationElement declarationOrType )
        where T : class, ICompilationElement
        => declarationOrType switch
        {
            IAttribute => throw new NotSupportedException( AttributeRef.CannotBeMadeDurableMessage ),
            IType type => new TypeIdRef<T>( type.GetSerializableTypeId( includeGenericContext: true ) ),
            IDeclaration declaration => new DeclarationIdRef<T>( declaration.GetSerializableId() ),
            _ => throw new NotSupportedException(
                $"Cannot create a durable reference to a '{declarationOrType.DeclarationKind}' because it is neither a declaration nor a type." )
        };
}
