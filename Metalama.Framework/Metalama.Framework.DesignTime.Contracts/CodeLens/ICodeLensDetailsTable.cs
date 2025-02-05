// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Runtime.InteropServices;

namespace Metalama.Framework.DesignTime.Contracts.CodeLens;

[ComImport]
[Guid( "1516E7C1-8076-4226-9999-C1C961E08E0A" )]
public interface ICodeLensDetailsTable : ICodeLensDetails
{
    ICodeLensDetailsHeader[] Headers { get; }

    ICodeLensDetailsEntry[] Entries { get; }
}