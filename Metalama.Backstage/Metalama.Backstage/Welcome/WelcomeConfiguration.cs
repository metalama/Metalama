// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Configuration;
using System;

namespace Metalama.Backstage.Welcome;

[ConfigurationFile( "welcome.json" )]
[UsedImplicitly( ImplicitUseTargetFlags.WithMembers )]
internal record WelcomeConfiguration : ConfigurationFile
{
    public bool IsFirstStart { get; init; } = true;

    // This property is no longer used but we keep it here so that users don't get warnings during deserialization.
    [Obsolete]
    public bool IsFirstTimeEvaluationLicenseRegistrationPending { get; init; } = true;

    public bool WelcomePageDisplayed { get; init; }

    // This property is no longer used but we keep it here so that users don't get warnings during deserialization.
    [Obsolete]
    public bool IsWelcomePagePending { get; init; } = true;
}