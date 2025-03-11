// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.GenericContexts;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CodeModel.References;

/// <summary>
/// Represents an <see cref="IRef{T}"/> that is bound to a compilation and therefore several additional members.
/// </summary>
internal interface IFullRef : IRefImpl
{
    ISymbol GetClosestContainingSymbol();

    SyntaxTree? PrimarySyntaxTree { get; }

    /// <summary>
    /// Gets the <see cref="DeclarationKind"/> of the reference declaration, if available.
    /// </summary>
    DeclarationKind DeclarationKind { get; }

    IFullRef? ContainingDeclaration { get; }

    IFullRef<INamedType>? DeclaringType { get; }

    /// <summary>
    /// Gets the name of the referenced declaration, if available. 
    /// </summary>
    string? Name { get; }

    new IFullRef<TOut> As<TOut>()
        where TOut : class, ICompilationElement;

    /// <summary>
    /// Gets the parent factory.
    /// </summary>
    RefFactory RefFactory { get; }

    /// <summary>
    /// Gets all <see cref="AttributeData"/> on the target of the reference without resolving the reference to
    /// the code model.
    /// </summary>
    ResolvedAttributeRef GetAttributes();

    /// <summary>
    /// Gets a value indicating whether the current reference points to a definition, as opposed to a generic construct.
    /// </summary>
    bool IsDefinition { get; }

    /// <summary>
    /// Enumerates all attributes declared in source code or in the builder, but not introduced attributes.
    /// </summary>
    void EnumerateAttributes( CompilationModel compilation, Action<AttributeRef> add );

    /// <summary>
    /// Enumerates all interfaces implemented in source code or in the builder, but not introduced interfaces. Only for named types.
    /// </summary>
    void EnumerateAllImplementedInterfaces( Action<IFullRef<INamedType>> add );

    /// <summary>
    /// Enumerates all interfaces implemented in source code or in the builder, but not introduced interfaces. Only for named types.
    /// </summary>
    void EnumerateImplementedInterfaces( Action<IFullRef<INamedType>> add );

    /// <summary>
    /// Gets all members of a given name declared in source code (but not introduced ones). For builders, this returns an empty collection.
    /// </summary>
    IEnumerable<IFullRef> GetMembersOfName( string name, DeclarationKind kind, CompilationModel compilation );

    /// <summary>
    /// Gets all members declared in source code (but not introduced ones). For builders, this returns an empty collection.
    /// </summary>
    IEnumerable<IFullRef> GetMembers( DeclarationKind kind, CompilationModel compilation );
}

internal interface IFullRef<out T> : IFullRef, IRef<T>
    where T : class, ICompilationElement
{
    /// <summary>
    /// Gets a reference to the generic definition.
    /// </summary>
    IFullRef<T> DefinitionRef { get; }

    /// <summary>
    /// Gets a reference with a generic context, i.e. assigning type parameters.
    /// </summary>
    /// <param name="genericContext"></param>
    /// <returns></returns>
    IFullRef<T> WithGenericContext( GenericContext genericContext );

    /// <summary>
    /// Gets the <see cref="IDeclaration"/> in the canonical <see cref="CompilationModel"/> of the current <see cref="RefFactory"/>.
    /// Try to use this property instead of <see cref="ConstructedDeclaration"/> when the generic context does not matter.
    /// </summary>
    T Definition { get; }

    /// <summary>
    /// Gets the <see cref="IDeclaration"/> in the canonical <see cref="CompilationModel"/> of the current <see cref="RefFactory"/>,
    /// constructed for the <see cref="GenericContext"/>. Use the <see cref="Definition"/> whenever possible to avoid allocating too many instances.
    /// </summary>
    T ConstructedDeclaration { get; }
}