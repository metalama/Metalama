// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;

namespace Metalama.Backstage.Utilities;

/// <summary>
/// The strategies that <see cref="AtomicFileWriter"/> may use when <see cref="System.IO.File.Replace(string,string,string)"/>
/// fails. Each of them gives up a guarantee of <c>File.Replace</c>, so each must be allowed explicitly.
/// </summary>
[Flags]
[PublicAPI]
public enum AtomicFileWriterFallbacks
{
    /// <summary>
    /// Only <see cref="System.IO.File.Replace(string,string,string)"/> is used, which preserves the access control list of the
    /// destination.
    /// </summary>
    None = 0,

    /// <summary>
    /// Renames the temporary file over the destination. The substitution remains atomic, but the destination takes the
    /// access control list inherited by the temporary file, so explicit restrictions on the destination are lost.
    /// </summary>
    RenameOverwrite = 1,

    /// <summary>
    /// Writes the destination in place. Its access control list is preserved, but a concurrent reader can observe a
    /// partially written file.
    /// </summary>
    WriteInPlace = 2,

    /// <summary>
    /// Every fallback.
    /// </summary>
    All = RenameOverwrite | WriteInPlace
}
