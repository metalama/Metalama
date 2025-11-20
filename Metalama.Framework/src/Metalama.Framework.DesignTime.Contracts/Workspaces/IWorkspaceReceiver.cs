// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Metalama.Framework.DesignTime.Contracts.Workspaces;

/// <summary>
/// Exposes the <see cref="SetWorkspace"/> method accepting the <c>VisualStudioWorkspace</c> instance.
/// </summary>
[ComImport]
[Guid( "b2a4a92a-8b0f-45d4-bad7-bc5e9a370fc0" )]
public interface IWorkspaceReceiver
{
    /// <summary>
    /// Sets the <c>VisualStudioWorkspace</c> instance.
    /// </summary>
    void SetWorkspace( Workspace workspace );
}