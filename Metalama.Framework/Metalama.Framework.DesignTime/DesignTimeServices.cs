// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using Metalama.Compiler;
using Metalama.Framework.Engine.Utilities.Diagnostics;

namespace Metalama.Framework.DesignTime;

internal static class DesignTimeServices
{
    public static void Initialize()
    {
        if ( MetalamaCompilerInfo.IsActive )
        {
            throw new InvalidOperationException( "This method cannot be called from the Metalama Compiler process." );
        }

        BackstageServiceFactoryInitializer.Initialize(
            new BackstageInitializationOptions( new MetalamaDesignTimeApplicationInfo() )
            {
                AddSupportServices = true, AddUserInterface = true, AddLicensing = false
            } );
    }
}