// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Metalama.Framework.Engine.Extensibility.Transformations;

/// <summary>
/// The kinds of <see cref="RedirectedArgument"/>.
/// </summary>
[PublicAPI]
public enum RedirectedArgumentKind
{
    /// <summary>
    /// The receiver of the source call site.
    /// </summary>
    SourceReceiver,

    /// <summary>
    /// An argument written at the source call site.
    /// </summary>
    SourceArgument,

    /// <summary>
    /// An expression emitted at the call site.
    /// </summary>
    Value
}
