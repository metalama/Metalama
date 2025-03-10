// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Contracts.EntryPoint;
using Microsoft.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.DesignTime.Contracts.CodeLens;

/// <summary>
/// Exposes the methods that implement the code lens editor feature.
/// </summary>
[ComImport]
[Guid( "9E3E6194-302E-4F36-8612-FD2CA0190F21" )]
public interface ICodeLensService : ICompilerService
{
    /// <summary>
    /// Gets the summary text inlined inside the editor.
    /// </summary>
    Task GetCodeLensSummaryAsync( Compilation compilation, ISymbol symbol, ICodeLensSummary?[] result, CancellationToken cancellationToken = default );

    /// <summary>
    /// Gets the detailed content displayed when the user clicks on the summary text.
    /// </summary>
    Task GetCodeLensDetailsAsync( Compilation compilation, ISymbol symbol, ICodeLensDetails?[] result, CancellationToken cancellationToken = default );
}