// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Runtime.InteropServices;

namespace Metalama.Framework.DesignTime.Contracts.CodeLens;

/// <summary>
/// Represents a column header in a <see cref="ICodeLensDetailsTable"/>.
/// </summary>
[ComImport]
[Guid( "4BBCF97F-A51E-4D2D-A2BD-3C639FBACC80" )]
public interface ICodeLensDetailsHeader
{
    string DisplayName { get; }

    bool IsVisible { get; }

    string UniqueName { get; }

    /// <summary>
    /// Gets the column width. For details, see the <c>CodeLensDetailHeaderDescriptor.Width</c> property in the VS SDK documentation.
    /// </summary>
    double Width { get; }
}