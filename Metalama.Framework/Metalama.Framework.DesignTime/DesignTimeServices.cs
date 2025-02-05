// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
                AddSupportServices = true, AddUserInterface = true, AddLicensing = true
            } );
    }
}