// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;
using Newtonsoft.Json;
using System.Collections.Immutable;
using System.ComponentModel;

namespace Metalama.Framework.DesignTime.Diagnostics
{
    /// <summary>
    /// A JSON-serializable file that contains user-defined diagnostic and suppression descriptors.
    /// </summary>
    [ConfigurationFile( "userDiagnostics.json" )]
    [Description( "Stores the IDs of diagnostics and suppressions defined by user aspects." )]
    [JsonObject]
    public sealed record UserDiagnosticsConfiguration : ConfigurationFile
    {
        public ImmutableDictionary<string, UserDiagnosticRegistration> Diagnostics { get; init; } =
            ImmutableDictionary<string, UserDiagnosticRegistration>.Empty.WithComparers( StringComparer.Ordinal );

        public ImmutableHashSet<string> Suppressions { get; init; } = ImmutableHashSet<string>.Empty.WithComparer( StringComparer.Ordinal );
    }
}