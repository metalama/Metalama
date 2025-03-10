// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Utilities;

namespace Metalama.Framework.Code;

/// <summary>
/// Represents a context in which type parameters are mapped to their values.
/// </summary>
[CompileTime]
[InternalImplement]
public interface IGenericContext
{
    /// <summary>
    /// Gets a value indicating whether the current context contains any non-trivial mapping.
    /// This value is <c>true</c> if there is no type parameter in the context of the current declaration
    /// or if the context is unbound, i.e. in the context of a generic type definition.
    /// </summary>
    bool IsEmptyOrIdentity { get; }
}