// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Contracts.EntryPoint;
using System.Runtime.InteropServices;

namespace Metalama.Framework.DesignTime.Contracts.ServiceHub;

[ComImport]
[Guid( "B8DAD9AE-CF7F-4E70-863C-E434272023DD" )]
public interface IServiceHubLocator : ICompilerService
{
    IServiceHubInfo ServiceHubInfo { get; }
}