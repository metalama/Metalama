// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Backstage.Maintenance;

public enum TempFileVersionScope
{
    Default,  // This is the version of IApplicationInfo.
    None,     // This file is not version-dependent.
    Backstage // The version of the Backstage component should be used instead.
}