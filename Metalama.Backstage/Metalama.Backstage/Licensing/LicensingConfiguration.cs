// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Immutable;

namespace Metalama.Backstage.Licensing;

[ConfigurationFile( "licensing.json" )]
internal sealed record LicensingConfiguration : ConfigurationFile
{
    /// <summary>
    /// Gets the date of the last evaluation period.
    /// </summary>
    [JsonProperty( "lastEvaluationStartDate" )]
    public DateTime? LastEvaluationStartDate { get; init; }

    // Usually, there should be only one licenses, but we want to support a 2025.0 or older Free license (gen 0) together with 2025.1 or newer Community license (gen 1).
    // So gen 0 licenses are stored in the `license` property for backwards compatiblity and gen 1 licenses are stored in the `licenses` property.

    [JsonProperty( "license" )]
    public string? License { get; init; }

    [JsonProperty( "licenses" )]
    public ImmutableArray<string> Licenses { get; init; } = ImmutableArray<string>.Empty;
}