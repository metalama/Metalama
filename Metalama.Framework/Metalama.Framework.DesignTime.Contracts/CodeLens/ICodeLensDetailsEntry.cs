// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Runtime.InteropServices;

namespace Metalama.Framework.DesignTime.Contracts.CodeLens;

/// <summary>
/// Represents an entry (or line) in a <see cref="ICodeLensDetailsTable"/>.
/// </summary>
[ComImport]
[Guid( "3903FF85-40C4-4158-9A38-CA5C9CC084CA" )]
public interface ICodeLensDetailsEntry
{
    ICodeLensDetailsField[] Fields { get; }

    string? Tooltip { get; }
}