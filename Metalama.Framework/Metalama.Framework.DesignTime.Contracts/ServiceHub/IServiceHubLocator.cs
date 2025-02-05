// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Contracts.EntryPoint;
using System.Runtime.InteropServices;

namespace Metalama.Framework.DesignTime.Contracts.ServiceHub;

[ComImport]
[Guid( "B8DAD9AE-CF7F-4E70-863C-E434272023DD" )]
public interface IServiceHubLocator : ICompilerService
{
    IServiceHubInfo ServiceHubInfo { get; }
}