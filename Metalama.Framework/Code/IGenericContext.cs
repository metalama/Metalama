// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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