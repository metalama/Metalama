// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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