// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code.Comparers;

namespace Metalama.Framework.Code;

/// <summary>
/// Represents a reference to an <see cref="IDeclaration"/> or <see cref="IType"/>, which is valid across different compilation versions
/// (i.e. <see cref="ICompilation"/>) and, when serialized, across projects and processes. References can be resolved using
/// <see cref="RefExtensions.GetTarget{T}(Metalama.Framework.Code.IRef{T},Metalama.Framework.Code.ICompilation,Metalama.Framework.Code.IGenericContext?)"/>.
/// </summary>
/// <typeparam name="T">The type of the target object of the declaration or type.</typeparam>
/// <remarks>
/// <para>Use <see cref="RefEqualityComparer{T}"/> to compare instances of <see cref="IRef"/>.</para>
/// </remarks>
public interface IRef<out T> : IRef
    where T : class, ICompilationElement { }