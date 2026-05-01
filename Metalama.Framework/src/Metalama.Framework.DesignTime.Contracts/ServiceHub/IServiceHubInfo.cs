// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Runtime.InteropServices;

namespace Metalama.Framework.DesignTime.Contracts.ServiceHub;

[ComImport]
[Guid( "DF409271-4E82-439D-9BF9-7D8C93D43B31" )]
public interface IServiceHubInfo
{
    string PipeName { get; }

    Version Version { get; }
}