// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code.Comparers;
using Metalama.Framework.Utilities;
using System;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// Represents a reference to an <see cref="IDeclaration"/> or <see cref="IType"/> that remains valid across different
    /// compilation versions (i.e., <see cref="ICompilation"/>) and, when serialized, across projects and processes.
    /// All objects implementing this interface also implement the strongly-typed <see cref="IRef{T}"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// References are essential in Metalama because each <see cref="IDeclaration"/> or <see cref="IType"/> object is bound
    /// to a specific <see cref="ICompilation"/>. As aspects execute, the compilation evolves through multiple immutable
    /// versions at each pipeline step. References provide a stable way to identify the same declaration across these
    /// compilation versions.
    /// </para>
    /// <para>
    /// <b>Common use cases:</b>
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description>Storing declaration references in aspect fields for use across compilation versions.</description>
    /// </item>
    /// <item>
    /// <description>Serializing declaration references for cross-project scenarios (inheritable aspects,
    /// reference validators).</description>
    /// </item>
    /// <item>
    /// <description>Passing declaration references to child aspects or storing them in <see cref="IAspectState"/>.</description>
    /// </item>
    /// </list>
    /// <para>
    /// To obtain a reference, call <see cref="IDeclaration.ToRef"/> or <see cref="IType.ToRef"/>. To resolve a reference
    /// back to the declaration in a specific compilation, use
    /// <see cref="RefExtensions.GetTarget{T}(IRef{T},ICompilation,IGenericContext?)"/> or
    /// <see cref="RefExtensions.GetTarget{T}(IRef{T})"/> (for the current execution context).
    /// </para>
    /// <para>
    /// Use <see cref="RefEqualityComparer{T}"/> to compare instances of <see cref="IRef"/> in collections.
    /// </para>
    /// </remarks>
    /// <seealso cref="IRef{T}"/>
    /// <seealso cref="RefExtensions"/>
    /// <seealso cref="RefEqualityComparer{T}"/>
    /// <seealso cref="RefComparison"/>
    /// <seealso cref="SerializableDeclarationId"/>
    /// <seealso href="@aspect-serialization"/>
    [CompileTime]
    [InternalImplement]
    public interface IRef : IEquatable<IRef>
    {
        /// <summary>
        /// Returns a string that uniquely identifies the declaration represented by the current reference. This identifier can then be resolved using <see cref="IDeclarationFactory.GetDeclarationFromId"/>, even in
        /// a different process or with a different version of Metalama than the one that created the id.
        /// </summary>
        /// <returns>A string, or <c>null</c> if the current reference cannot be serialized to a public id.</returns>
        SerializableDeclarationId ToSerializableId();

        /// <summary>
        /// Changes the reference type. This method can be used in two scenarios: instead of a C# cast with durable references (see <see cref="IsDurable"/>),
        /// or between <see cref="IField"/> and <see cref="IProperty"/> when a field is overridden into a property (see <see cref="IField.OverridingProperty"/>
        /// and <see cref="IProperty.OriginalField"/>).
        /// </summary>
        IRef<TOut> As<TOut>()
            where TOut : class, ICompilationElement;

        /// <summary>
        /// Gets a value indicating whether the reference stores only a string identifier rather than holding
        /// a reference to the compilation state. This is an internal concept with no user-facing scenario;
        /// there is no public API to create durable references.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Non-durable references</b> (when <c>IsDurable</c> is <c>false</c>) are bound to a specific compilation
        /// context. They are faster to resolve because they have direct access to the underlying symbol, but they
        /// prevent that compilation from being garbage-collected as long as the reference is held in memory.
        /// References returned by <see cref="IDeclaration.ToRef"/> are always non-durable.
        /// </para>
        /// <para>
        /// <b>Durable references</b> (when <c>IsDurable</c> is <c>true</c>) store only a string-based identifier.
        /// They are slower to resolve but do not hold any reference to the compilation. There is no public API
        /// to create durable references; they are used internally at design time to persist references between
        /// IDE compilation updates without causing memory leaks.
        /// </para>
        /// </remarks>
        bool IsDurable { get; }

        /// <summary>
        /// Compares this reference to another reference using the specified comparison strategy.
        /// </summary>
        /// <param name="other">The other reference to compare to.</param>
        /// <param name="comparison">The comparison strategy to use. See <see cref="RefComparison"/> for available options.</param>
        /// <returns><c>true</c> if the references are equal according to the specified comparison strategy; otherwise, <c>false</c>.</returns>
        /// <seealso cref="RefComparison"/>
        /// <seealso cref="RefEqualityComparer{T}"/>
        bool Equals( IRef? other, RefComparison comparison = RefComparison.Default );

        /// <summary>
        /// Returns a hash code for this reference using the specified comparison strategy.
        /// </summary>
        /// <param name="comparison">The comparison strategy to use. See <see cref="RefComparison"/> for available options.</param>
        /// <returns>A hash code compatible with the specified comparison strategy.</returns>
        /// <seealso cref="RefComparison"/>
        /// <seealso cref="RefEqualityComparer{T}"/>
        int GetHashCode( RefComparison comparison );

        /// <summary>
        /// Gets the target of the reference for a given compilation, with control over the interface type and error handling.
        /// Prefer the extension methods <see cref="RefExtensions.GetTarget{T}(IRef{T},ICompilation,IGenericContext?)"/>
        /// or <see cref="RefExtensions.GetTargetOrNull{T}(IRef{T},ICompilation,IGenericContext?)"/> for typical usage.
        /// </summary>
        /// <param name="compilation">The compilation in which to resolve the reference.</param>
        /// <param name="interfaceType">The optional interface type to use for the target, or <c>null</c> to use the default.</param>
        /// <param name="genericContext">The optional generic context for resolving generic instances.</param>
        /// <param name="throwIfMissing">
        /// If <c>true</c>, throws an exception when the reference cannot be resolved; if <c>false</c>, returns <c>null</c>.
        /// </param>
        /// <returns>
        /// The resolved compilation element, or <c>null</c> if the reference cannot be resolved and
        /// <paramref name="throwIfMissing"/> is <c>false</c>.
        /// </returns>
        ICompilationElement? GetTargetInterface(
            ICompilation compilation,
            Type? interfaceType,
            IGenericContext? genericContext = null,
            bool throwIfMissing = false );

        // GetTargetInterface is intentionally in the IRef (and not in some IRefInternal) to avoid casts because we are in a performance-critical path.
        // It is named differently than GetTarget to avoid name resolution problems.
    }
}