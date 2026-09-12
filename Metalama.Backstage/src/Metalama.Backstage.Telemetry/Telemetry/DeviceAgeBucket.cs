// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Backstage.Telemetry;

internal enum DeviceAgeBucket
{
    // Do not rename, or if you do, ensure that the same literal names are transferred to Matomo.
    None,
    LessThan1,
    From1To30,
    MoreThan30
}