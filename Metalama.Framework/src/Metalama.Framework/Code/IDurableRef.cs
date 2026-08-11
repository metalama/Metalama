// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Utilities;

namespace Metalama.Framework.Code;

/// <summary>
/// An <see cref="IRef"/> that may be stored in an object outliving the run that produced it, without keeping a version
/// of the compilation in memory for as long as it is held. Its <see cref="IRef.IsDurable"/> property is always
/// <c>true</c>.
/// </summary>
/// <remarks>
/// <para>
/// Use this type in the signature of anything that stores a reference for longer than a single pipeline run: a field
/// of a fabric, a field of an inheritable aspect, or a parameter of an API that keeps what it is given. The compiler
/// then enforces what would otherwise only be a comment, because an ordinary <see cref="IRef{T}"/> obtained from
/// <see cref="IDeclaration.ToRef"/> holds the compilation and cannot be converted implicitly.
/// </para>
/// <para>
/// Call <see cref="RefExtensions.ToDurableRef{T}"/> on a declaration or a type to obtain one, or
/// <see cref="IRef.ToDurable"/> when a reference is already at hand. Resolving a durable reference may cost an
/// identifier lookup, which is why references are not durable by default.
/// </para>
/// <para>
/// What such a reference stores is not part of this contract, and depends on the kind of compilation: a string
/// identifier wherever compilations succeed one another, as they do at design time, and the reference it was made from
/// during a batch compilation, whose single compilation outlives the build. Both are durable in the sense that matters,
/// which is that holding one is safe. See <see cref="IRef.IsDurable"/>.
/// </para>
/// </remarks>
/// <seealso cref="IDurableRef{T}"/>
/// <seealso cref="IRef.IsDurable"/>
/// <seealso cref="IRef.ToDurable"/>
/// <seealso cref="RefExtensions.ToDurableRef{T}"/>
[CompileTime]
[InternalImplement]
public interface IDurableRef : IRef { }
