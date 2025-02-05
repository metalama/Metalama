// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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