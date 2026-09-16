// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Immutable;

namespace Metalama.Backstage.Telemetry;

/// <summary>
/// The "everything off" <see cref="ITelemetryPolicy"/>: every scenario resolves to <see cref="TelemetryConsent.No"/>.
/// </summary>
public sealed class NullTelemetryPolicy : ITelemetryPolicy
{
    private readonly TelemetryDisabledReason _reason;

    public static ITelemetryPolicy NoContext { get; } = new NullTelemetryPolicy( TelemetryDisabledReason.NoRepositoryContext, false );

    private NullTelemetryPolicy( TelemetryDisabledReason reason, bool hasRepositoryContext )
    {
        this._reason = reason;
        this.HasRepositoryContext = hasRepositoryContext;
    }

    public bool HasRepositoryContext { get; }

    public TelemetryConsent GetConsent( TelemetryScenario scenario ) => TelemetryConsent.No;

    public (TelemetryConsent Consent, TelemetryDisabledReason Reason) GetConsentAndReason( TelemetryScenario scenario ) => (TelemetryConsent.No, this._reason);

    // The null policy disables telemetry because there is no context/repository at all — not because a repository opted
    // out. So "blocked by metalama.json" is false here.

    public ImmutableArray<TelemetryContextWarning> Warnings => ImmutableArray<TelemetryContextWarning>.Empty;
}