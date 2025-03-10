// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;

namespace Metalama.Backstage.Licensing.Consumption.Sources;

[PublicAPI]
[Flags]
public enum LicenseSourceKind
{
    None,
    Unattended = 1,
    UserProfile = 2,
    Project = 4,
    Test = 8,
    All = Unattended | UserProfile | Project | Test
}