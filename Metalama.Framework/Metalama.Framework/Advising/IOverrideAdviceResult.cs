// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;

namespace Metalama.Framework.Advising;

/// <summary>
/// Represents the result of the <c>Override</c> methods of the <see cref="IAdviceFactory"/> interface.
/// </summary>
public interface IOverrideAdviceResult<out T> : IAdviceResult
    where T : class, IDeclaration
{
    /// <summary>
    /// Gets the declaration transformed by the advice method. For advice that modify a field,
    /// this is the property that now represents the field.
    /// </summary>
    T Declaration { get; }
}