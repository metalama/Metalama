// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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