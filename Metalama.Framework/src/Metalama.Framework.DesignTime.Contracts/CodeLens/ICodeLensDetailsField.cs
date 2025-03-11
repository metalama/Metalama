// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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