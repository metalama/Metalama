// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Licensing.Audit;
using Metalama.Backstage.Testing;
using System;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Licensing.Audit;

public sealed class LicenseAuditLedgerTests : TestsBase
{
    public LicenseAuditLedgerTests( ITestOutputHelper logger ) : base( logger ) { }

    /// <summary>
    /// Verifies that the "one audit per license per day" deduplication still works across a calendar-day boundary
    /// (acceptance criterion of #1679): the dedup window is a rolling 24 hours, even though the state is stored in
    /// per-day files.
    /// </summary>
    [Fact]
    public void DedupWorksWithinAndAcrossDayBoundary()
    {
        this.Time.Set( new DateTime( 2026, 1, 1, 0, 0, 0, DateTimeKind.Utc ) );

        var ledger = new LicenseAuditLedger( this.ServiceProvider );
        const long hash = 0x1234_5678_9ABC_DEF0;

        // First audit is performed.
        Assert.True( ledger.TryRegisterAudit( hash ) );

        // Same instant: deduplicated.
        Assert.False( ledger.TryRegisterAudit( hash ) );

        // One millisecond before 24 hours: still deduplicated (same and next calendar day must be considered).
        this.Time.AddTime( TimeSpan.FromDays( 1 ) - TimeSpan.FromMilliseconds( 1 ) );
        Assert.False( ledger.TryRegisterAudit( hash ) );

        // Exactly 24 hours after the first audit (new calendar day): a new audit must be performed.
        this.Time.AddTime( TimeSpan.FromMilliseconds( 1 ) );
        Assert.True( ledger.TryRegisterAudit( hash ) );

        // ...and immediately deduplicated again.
        Assert.False( ledger.TryRegisterAudit( hash ) );
    }

    /// <summary>
    /// Verifies that the dedup ledger is stored as one file per UTC day under the telemetry tree, so old day-files
    /// can be purged by the telemetry retention sweep.
    /// </summary>
    [Fact]
    public void LedgerIsStoredAsPerDayFiles()
    {
        var standardDirectories = this.ServiceProvider.GetRequiredBackstageService<IStandardDirectories>();

        this.Time.Set( new DateTime( 2026, 1, 1, 12, 0, 0, DateTimeKind.Utc ) );

        var ledger = new LicenseAuditLedger( this.ServiceProvider );

        Assert.True( ledger.TryRegisterAudit( 1 ) );

        // Move to the next day and register a different license.
        this.Time.AddTime( TimeSpan.FromDays( 1 ) );
        Assert.True( ledger.TryRegisterAudit( 2 ) );

        var files = this.FileSystem.GetFiles( standardDirectories.TelemetryAuditLedgerDirectory )
            .Select( Path.GetFileName )
            .OrderBy( x => x, StringComparer.Ordinal )
            .ToArray();

        Assert.Equal( new[] { "audit-20260101.json", "audit-20260102.json" }, files );
    }
}
