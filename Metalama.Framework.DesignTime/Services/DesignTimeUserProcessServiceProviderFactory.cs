// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Contracts.EntryPoint;

namespace Metalama.Framework.DesignTime.Services;

internal class DesignTimeUserProcessServiceProviderFactory : DesignTimeServiceProviderFactory
{
    public DesignTimeUserProcessServiceProviderFactory( DesignTimeEntryPointManager? entryPointManager )
        : base( entryPointManager, DesignTimeProcessKind.VsUserProcess ) { }
}