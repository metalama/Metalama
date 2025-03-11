// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Runtime.InteropServices;

namespace Metalama.Framework.DesignTime.Contracts.CodeLens;

[ComImport]
[Guid( "90EE87E4-68CD-43FA-996F-FD0AE6691610" )]
public interface ICodeLensSummary
{
    string Description { get; }

    string? TooltipText { get; }
}