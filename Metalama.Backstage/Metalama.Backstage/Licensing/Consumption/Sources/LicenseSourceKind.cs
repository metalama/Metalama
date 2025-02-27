// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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