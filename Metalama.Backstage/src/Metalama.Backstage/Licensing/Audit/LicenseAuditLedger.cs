// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace Metalama.Backstage.Licensing.Audit;

/// <summary>
/// Stores the "one audit per license per day" deduplication state as time-bucketed (per-day) files under the
/// telemetry tree, replacing the single unbounded <c>LastAuditTimes</c> dictionary. Old day-files are purged by
/// the same telemetry retention sweep as the rest of the tree, while the dedup check only ever needs the current
/// and previous day-files (the rolling 24-hour window spans at most two calendar days).
/// </summary>
internal sealed class LicenseAuditLedger
{
    private readonly IFileSystem _fileSystem;
    private readonly IDateTimeProvider _time;
    private readonly ILogger _logger;
    private readonly string _ledgerDirectory;

    public LicenseAuditLedger( IServiceProvider serviceProvider )
    {
        this._fileSystem = serviceProvider.GetRequiredBackstageService<IFileSystem>();
        this._time = serviceProvider.GetRequiredBackstageService<IDateTimeProvider>();
        this._logger = serviceProvider.GetLoggerFactory().Licensing();
        this._ledgerDirectory = serviceProvider.GetRequiredBackstageService<IStandardDirectories>().TelemetryAuditLedgerDirectory;
    }

    private string GetLedgerFilePath( DateTime utcDay )
        => Path.Combine( this._ledgerDirectory, $"audit-{utcDay.ToString( "yyyyMMdd", CultureInfo.InvariantCulture )}.json" );

    /// <summary>
    /// Atomically checks whether the license identified by <paramref name="auditHashCode"/> has been audited within
    /// the last day and, if not, records the audit in the current day-file. Returns <c>true</c> if an audit must be
    /// performed.
    /// </summary>
    public bool TryRegisterAudit( long auditHashCode )
    {
        var now = this._time.UtcNow;

        // The whole read-check-write must be atomic across processes, matching the previous behavior backed by the
        // configuration manager's UpdateIf.
        using ( MutexHelper.WithGlobalLock( this._ledgerDirectory ) )
        {
            var todayFilePath = this.GetLedgerFilePath( now );
            var yesterdayFilePath = this.GetLedgerFilePath( now.AddDays( -1 ) );

            // Find the most recent audit time recorded for this license across the rolling 24-hour window.
            long? lastAuditTicks = null;

            foreach ( var filePath in new[] { yesterdayFilePath, todayFilePath } )
            {
                if ( this.TryReadLedger( filePath, out var entries )
                     && entries.TryGetValue( auditHashCode, out var ticks )
                     && (lastAuditTicks == null || ticks > lastAuditTicks) )
                {
                    lastAuditTicks = ticks;
                }
            }

            if ( lastAuditTicks != null && lastAuditTicks.Value > now.AddDays( -1 ).Ticks )
            {
                return false;
            }

            // Record the audit in the current day-file.
            this.TryReadLedger( todayFilePath, out var todayEntries );
            todayEntries[auditHashCode] = now.Ticks;
            this.WriteLedger( todayFilePath, todayEntries );

            return true;
        }
    }

    private bool TryReadLedger( string filePath, out Dictionary<long, long> entries )
    {
        entries = new Dictionary<long, long>();

        if ( !this._fileSystem.FileExists( filePath ) )
        {
            return false;
        }

        try
        {
            var content = this._fileSystem.ReadAllText( filePath );
            var serialized = JsonSerializer.Deserialize<Dictionary<string, long>>( content );

            if ( serialized != null )
            {
                foreach ( var pair in serialized )
                {
                    if ( long.TryParse( pair.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var hash ) )
                    {
                        entries[hash] = pair.Value;
                    }
                }
            }

            return true;
        }
        catch ( Exception e )
        {
            this._logger.Warning?.Log( $"Cannot read the license-audit ledger '{filePath}': {e.Message}" );

            return false;
        }
    }

    private void WriteLedger( string filePath, Dictionary<long, long> entries )
    {
        try
        {
            if ( !this._fileSystem.DirectoryExists( this._ledgerDirectory ) )
            {
                this._fileSystem.CreateDirectory( this._ledgerDirectory );
            }

            var serialized = new Dictionary<string, long>( entries.Count );

            foreach ( var pair in entries )
            {
                serialized[pair.Key.ToString( CultureInfo.InvariantCulture )] = pair.Value;
            }

            this._fileSystem.WriteAllText( filePath, JsonSerializer.Serialize( serialized ) );
        }
        catch ( Exception e )
        {
            this._logger.Warning?.Log( $"Cannot write the license-audit ledger '{filePath}': {e.Message}" );
        }
    }
}
