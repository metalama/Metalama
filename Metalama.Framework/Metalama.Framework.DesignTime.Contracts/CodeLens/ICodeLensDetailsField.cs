// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Runtime.InteropServices;

namespace Metalama.Framework.DesignTime.Contracts.CodeLens;

/// <summary>
/// Represents a field (or cell) in a <see cref="ICodeLensDetailsTable"/>.
/// </summary>
[ComImport]
[Guid( "AD813C57-3CB5-40D9-A553-D46A4790FCD5" )]
public interface ICodeLensDetailsField
{
    string Text { get; }
}