// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Utilities;

namespace Metalama.Framework.Code;

/// <summary>
/// A strongly-typed <see cref="IRef{T}"/> that stores only a string identifier and therefore does not keep the
/// compilation it came from in memory.
/// </summary>
/// <typeparam name="T">The type of the target object, such as <see cref="IMethod"/>, <see cref="IProperty"/>,
/// <see cref="INamedType"/>, or any other <see cref="ICompilationElement"/>.</typeparam>
/// <remarks>
/// This is the strongly-typed variant of <see cref="IDurableRef"/>, which documents when to use it. Obtain one by
/// calling <see cref="RefExtensions.ToDurableRef{T}"/> on a declaration or a type, or <see cref="IRef{T}.ToDurable"/>
/// on a reference.
/// </remarks>
/// <seealso cref="IDurableRef"/>
/// <seealso cref="IRef{T}.ToDurable"/>
/// <seealso cref="RefExtensions.ToDurableRef{T}"/>
[CompileTime]
[InternalImplement]
public interface IDurableRef<out T> : IRef<T>, IDurableRef
    where T : class, ICompilationElement { }
