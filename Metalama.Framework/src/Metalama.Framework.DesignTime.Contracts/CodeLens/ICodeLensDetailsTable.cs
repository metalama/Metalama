// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Runtime.InteropServices;

namespace Metalama.Framework.DesignTime.Contracts.CodeLens;

[ComImport]
[Guid( "9046938A-AF29-4AC2-AF46-20EF5818238A" )]
public interface ICodeLensDetailsTable : ICodeLensDetails
{
    ICodeLensDetailsHeader[] Headers { get; }

    ICodeLensDetailsEntry[] Entries { get; }
}