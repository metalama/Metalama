// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Contracts.EntryPoint;
using System.Runtime.InteropServices;

namespace Metalama.Framework.DesignTime.Contracts.ServiceHub;

[ComImport]
[Guid( "B31B898F-3018-4F73-A1C6-87AE7EE44A02" )]
public interface IServiceHubLocator : ICompilerService
{
    IServiceHubInfo ServiceHubInfo { get; }
}