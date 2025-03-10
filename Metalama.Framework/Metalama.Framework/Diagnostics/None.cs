// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Diagnostics
{
    /// <summary>
    /// A type to be used as generic argument of <see cref="DiagnosticDefinition{T}"/> when there is no parameter in the message.
    /// </summary>
    [CompileTime]
    public readonly struct None;
}