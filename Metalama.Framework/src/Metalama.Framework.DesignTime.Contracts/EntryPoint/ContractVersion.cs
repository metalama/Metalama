// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System.Runtime.InteropServices;

namespace Metalama.Framework.DesignTime.Contracts.EntryPoint;

[Guid( "8A5841E3-5D21-495C-99D8-280558B3A7BD" )]
[PublicAPI]
public struct ContractVersion
{
    public string Version;
    public int Revision;
}