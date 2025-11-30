// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code.Comparers;

namespace Metalama.Framework.Code;

/// <summary>
/// Represents a strongly-typed reference to an <see cref="IDeclaration"/> or <see cref="IType"/> that remains valid across
/// different compilation versions (i.e., <see cref="ICompilation"/>) and, when serialized, across projects and processes.
/// </summary>
/// <typeparam name="T">The type of the target object, such as <see cref="IMethod"/>, <see cref="IProperty"/>,
/// <see cref="INamedType"/>, or any other <see cref="ICompilationElement"/>.</typeparam>
/// <remarks>
/// <para>
/// This is the strongly-typed variant of <see cref="IRef"/>. To obtain a reference, call <see cref="IDeclaration.ToRef"/>
/// or the type-specific <c>ToRef()</c> method on any declaration interface. To resolve a reference back to the declaration
/// in a specific compilation, use <see cref="RefExtensions.GetTarget{T}(IRef{T},ICompilation,IGenericContext?)"/> or
/// <see cref="RefExtensions.GetTarget{T}(IRef{T})"/> (for the current execution context).
/// </para>
/// <para>
/// Use <see cref="RefEqualityComparer{T}"/> to compare instances of <see cref="IRef{T}"/> in collections.
/// </para>
/// </remarks>
/// <seealso cref="IRef"/>
/// <seealso cref="RefExtensions"/>
/// <seealso cref="RefEqualityComparer{T}"/>
/// <seealso href="@aspect-serialization"/>
public interface IRef<out T> : IRef
    where T : class, ICompilationElement { }