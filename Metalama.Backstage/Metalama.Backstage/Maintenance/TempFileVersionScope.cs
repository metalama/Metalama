// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Backstage.Maintenance;

public enum TempFileVersionScope
{
    Default,  // This is the version of IApplicationInfo.
    None,     // This file is not version-dependent.
    Backstage // The version of the Backstage component should be used instead.
}