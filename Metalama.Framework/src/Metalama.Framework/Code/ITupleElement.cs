// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Code;

/// <summary>
/// Represents an element of a tuple type.
/// </summary>
/// <seealso cref="ITupleType.TupleElements"/>
public interface ITupleElement : IField
{
    /// <summary>
    /// Gets the corresponding field in the underlying tuple structure, typically a field with a name <c>ItemN</c> where <c>N</c> is <see cref="Index"/><c> + 1</c>.
    /// </summary>
    IField CorrespondingTupleField { get; }

    /// <summary>
    /// Gets the zero-based index of the element in the tuple.
    /// </summary>
    int Index { get; }

    /// <summary>
    /// Gets a value indicating whether the element has a user-defined friendly name.
    /// </summary>
    bool HasFriendlyName { get; }
}