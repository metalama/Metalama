// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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