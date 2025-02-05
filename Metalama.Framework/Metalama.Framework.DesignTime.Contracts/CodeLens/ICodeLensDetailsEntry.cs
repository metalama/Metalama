// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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