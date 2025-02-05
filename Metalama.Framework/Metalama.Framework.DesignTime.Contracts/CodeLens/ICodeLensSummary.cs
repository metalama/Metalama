// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Runtime.InteropServices;

namespace Metalama.Framework.DesignTime.Contracts.CodeLens;

[ComImport]
[Guid( "90EE87E4-68CD-43FA-996F-FD0AE6691610" )]
public interface ICodeLensSummary
{
    string Description { get; }

    string? TooltipText { get; }
}