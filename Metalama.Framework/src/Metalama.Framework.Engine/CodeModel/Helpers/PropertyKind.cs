// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Engine.CodeModel.Helpers;

/// <summary>
/// Specifies the kind of a property based on its implementation.
/// </summary>
internal enum PropertyKind
{
    /// <summary>
    /// A regular property with explicit accessor bodies (not using a compiler-generated backing field).
    /// </summary>
    Default,

    /// <summary>
    /// An auto-implemented property with no explicit accessor bodies (e.g., <c>{ get; set; }</c>).
    /// The compiler generates the backing field and accessor implementations.
    /// </summary>
    Auto,

    /// <summary>
    /// A semi-automatic property using the C# 14 <c>field</c> keyword.
    /// The compiler generates the backing field, but accessor bodies are explicit.
    /// </summary>
    SemiAuto
}