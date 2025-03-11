// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.References;

namespace Metalama.Framework.Engine.Transformations;

/// <summary>
/// Represents a transformation that optionally replaces a member by itself.
/// </summary>
internal interface IReplaceMemberTransformation : ITransformation
{
    /// <summary>
    /// Gets a member that is replaced by this transformation or <c>null</c> if the transformation does not replace any member.
    /// </summary>
    IFullRef<IMember>? ReplacedMember { get; }

    // ReplacedMember must not be a reference because resolving the reference would returned the replacement, not the original member.
}