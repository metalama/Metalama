// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using System;

namespace Metalama.Backstage.Infrastructure;

/// <summary>
/// Base implementation of <see cref="IMachineIdProvider"/>. One derived class reads the identifier of one operating
/// system, and the service registration chooses the one that matches the current operating system.
/// </summary>
/// <remarks>
/// This class caches the identifier for the whole process, adds the error handling that every implementation needs,
/// and falls back to <see cref="Environment.MachineName"/> when the operating system reports no identifier.
/// </remarks>
internal abstract class MachineIdProvider : IMachineIdProvider
{
    // Reading the identifier costs a registry access, a file read or a child process, and its value cannot change
    // while the process runs, so it is read at most once per process even when several service providers are built.
    private static readonly object _sync = new();

    private static string? _cachedMachineId;

    protected ILogger Logger { get; }

    protected MachineIdProvider( IServiceProvider serviceProvider )
    {
        this.Logger = serviceProvider.GetLoggerFactory().GetLogger( this.GetType().Name );
    }

    public string MachineId
    {
        get
        {
            lock ( _sync )
            {
                return _cachedMachineId ??= this.GetUncachedMachineId();
            }
        }
    }

    /// <summary>
    /// Reads the identifier from the operating system, bypassing the cache. Tests call this method instead of
    /// <see cref="MachineId"/>, because the cache is shared by every instance in the process.
    /// </summary>
    internal string GetUncachedMachineId()
    {
        try
        {
            var machineId = this.ReadMachineId();

            if ( !string.IsNullOrWhiteSpace( machineId ) )
            {
                return machineId!.Trim();
            }

            this.Logger.Warning?.Log( "The operating system reports no machine identifier. Falling back to the machine name." );
        }
        catch ( Exception e )
        {
            // The identifier is only reported by telemetry, so no failure to read it may prevent the product from working.
            this.Logger.Warning?.Log( $"Cannot read the machine identifier from the operating system: {e.Message}" );
        }

        // The machine name is stable, and it is the only value left that identifies the machine. It is not guaranteed
        // to be unique, so a device count that includes such a machine is a lower bound.
        return Environment.MachineName;
    }

    /// <summary>
    /// Reads the identifier that the current operating system gives to the machine, or returns <c>null</c> when the
    /// operating system reports none. Exceptions do not have to be handled by the implementation.
    /// </summary>
    protected abstract string? ReadMachineId();
}
