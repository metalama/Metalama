// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine;
using Metalama.Framework.Engine.Utilities;
using System.Diagnostics;

namespace Metalama.Framework.DesignTime.VisualStudio.Rpc;

internal static class PipeNameProvider
{
    public static string GetPipeName( EndpointRole role, int? processId = null )
    {
        // The hash must be consistent across all target frameworks so we take the package version and not the MVID.
        var buildHash = HashUtilities.HashString( EngineAssemblyMetadataReader.Instance.PackageVersion.AssertNotNull() );

        return $"Metalama_{role.ToString().ToLowerInvariant()}_{processId ?? Process.GetCurrentProcess().Id}_{buildHash}";
    }
}