// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Code.Comparers;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Metrics;
using Metalama.Framework.Utilities;
using System;
using System.Collections.Immutable;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// Represent a declaration.
    /// </summary>
    /// <remarks>
    /// The <see cref="IDeclaration"/> interface implements <see cref="IEquatable{T}"/>. The implementation uses the <see cref="ICompilationComparers.Default"/> comparer.
    /// To use a different comparer, choose a different comparer from <see cref="IDeclaration"/>.<see cref="ICompilationElement.Compilation"/>.<see cref="ICompilation.Comparers"/>.
    /// </remarks>
    /// <seealso cref="DeclarationExtensions"/>
    /// <seealso cref="ICompilation"/>
    /// <seealso cref="INamedType"/>
    /// <seealso cref="IMember"/>
    /// <seealso cref="DeclarationKind"/>
    /// <seealso cref="IDeclarationOrigin"/>
    [CompileTime]
    public interface IDeclaration : IDisplayable, IDiagnosticLocation, ICompilationElement, IMeasurable, IEquatable<IDeclaration>
    {
        /// <summary>
        /// Gets a reference that can be used to identify the current declaration across different compilation versions.
        /// </summary>
        /// <returns>A compile-time serializable reference to the current declaration.</returns>
        /// <remarks>
        /// <para>
        /// References are essential in Metalama because <see cref="IDeclaration"/> objects are bound to a specific
        /// <see cref="ICompilation"/>. As aspects execute, the compilation evolves through multiple immutable versions.
        /// References provide a stable way to track the same declaration across these versions.
        /// </para>
        /// <para>
        /// <b>Common use cases:</b>
        /// </para>
        /// <list type="bullet">
        /// <item><description>Storing declaration references in aspect fields for use in later compilation versions.</description></item>
        /// <item><description>Serializing references for cross-project scenarios (inheritable aspects, validators).</description></item>
        /// <item><description>Passing references to child aspects via <see cref="IAspectState"/>.</description></item>
        /// </list>
        /// <para>
        /// To resolve a reference back to the declaration, use <see cref="RefExtensions.GetTarget{T}(IRef{T})"/> or
        /// <see cref="RefExtensions.GetTarget{T}(IRef{T},ICompilation,IGenericContext?)"/>.
        /// </para>
        /// </remarks>
        /// <seealso cref="IRef{T}"/>
        /// <seealso cref="RefExtensions"/>
        /// <seealso cref="ToSerializableId"/>
        /// <seealso href="@aspect-serialization"/>
        IRef<IDeclaration> ToRef();

        /// <summary>
        /// Gets a string-based identifier that uniquely identifies the current declaration within a compilation.
        /// </summary>
        /// <returns>A serializable declaration identifier.</returns>
        /// <remarks>
        /// <para>
        /// Unlike <see cref="ToRef"/>, which returns a strongly-typed reference object, this method returns a lightweight string-based
        /// identifier that can be persisted to disk or transmitted across processes. The identifier is guaranteed to
        /// be resolvable in a different process, even with a different version of Metalama.
        /// </para>
        /// <para>
        /// Use <see cref="SerializableDeclarationId.Resolve"/> or <see cref="IDeclarationFactory.GetDeclarationFromId"/>
        /// to resolve the identifier back to a declaration.
        /// </para>
        /// </remarks>
        /// <seealso cref="ToRef"/>
        /// <seealso cref="SerializableDeclarationId"/>
        /// <seealso href="@aspect-serialization"/>
        SerializableDeclarationId ToSerializableId();

        /// <summary>
        /// Gets the declaring assembly, which can be the current <see cref="ICompilationElement.Compilation"/>
        /// or a reference assembly.
        /// </summary>
        IAssembly DeclaringAssembly { get; }

        /// <summary>
        /// Gets the origin of the current declaration.
        /// </summary>
        IDeclarationOrigin Origin { get; }

        /// <summary>
        /// Gets the containing declaration, such as a <see cref="INamedType"/> for nested
        /// types or for methods. For non-nested types, returns the containing assembly
        /// (and not the namespace, use <see cref="INamedType.ContainingNamespace"/> for that).
        /// </summary>
        IDeclaration? ContainingDeclaration { get; }

        /// <summary>
        /// Gets the collection of custom attributes on the declaration.
        /// </summary>
        IAttributeCollection Attributes { get; }

        /// <summary>
        /// Gets a value indicating whether the member is implicitly declared, i.e. declared without being represented in source code.
        /// Returns <c>false</c> if it is explicitly declared in code.
        /// </summary>
        bool IsImplicitlyDeclared { get; }

        /// <summary>
        /// Gets the depth of the current declaration in the code model. The value of the <see cref="Depth"/> property has no absolute meaning,
        /// only a relative one, i.e. it is only relevant when comparing the depth of two declarations. A declaration has always a greater depth
        /// than the declaration in which it is contained. A type has always a greater depths than the base it derives from or the interfaces
        /// it implements.
        /// </summary>
        [Hidden]
        int Depth { get; }

        /// <summary>
        /// Gets a value indicating whether the current declaration is declared to the current project. It returns <c>false</c> for declarations
        /// declared in referenced projects or assemblies.
        /// </summary>
        bool BelongsToCurrentProject { get; }

        /// <summary>
        /// Gets the set of syntax nodes of the source code that declare the current declaration, or an empty
        /// set if the current declaration is not backed by source code.
        /// </summary>
        ImmutableArray<SourceReference> Sources { get; }

        /// <summary>
        /// Gets the <see cref="IGenericContext"/> for the current declaration.
        /// </summary>
        IGenericContext GenericContext { get; }
    }
}