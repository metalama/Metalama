// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Backstage.Configuration;
using System;
using System.Collections.Immutable;

namespace Metalama.Backstage.Licensing.Audit;

[ConfigurationFile( "audit.json" )]
[PublicAPI]
public record LicenseAuditConfiguration : ConfigurationFile
{
    public ImmutableDictionary<long, DateTime> LastAuditTimes { get; init; } =
        ImmutableDictionary<long, DateTime>.Empty;

    public DateTime? LastMatomoAuditTime { get; init; }

    [Obsolete]
    public bool IsFirstMatomoAudit { get; init; } = true;
}