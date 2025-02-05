// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Configuration;
using Newtonsoft.Json;
using System;

namespace Metalama.Backstage.Licensing;

[ConfigurationFile( "licensing.json" )]
internal record LicensingConfiguration : ConfigurationFile
{
    /// <summary>
    /// Gets the date of the last evaluation period.
    /// </summary>
    [JsonProperty( "lastEvaluationStartDate" )]
    public DateTime? LastEvaluationStartDate { get; init; }

    // Originally, the license configuration allowed for multiple license keys. We keep the array for backward compatibility,
    // but we no longer store more than one key there.
    [JsonProperty( "license" )]
    public string? License { get; init; }
}