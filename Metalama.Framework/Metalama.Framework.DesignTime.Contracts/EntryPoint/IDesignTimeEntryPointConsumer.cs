// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.DesignTime.Contracts.EntryPoint;

/// <summary>
/// Allows to retrieve instances of <see cref="ICompilerServiceProvider"/> from the <see cref="IDesignTimeEntryPointManager"/>.
/// </summary>
[ComImport]
[Guid( "B6EAF9AE-2A70-4BBB-93A1-C877E2758462" )]
public interface IDesignTimeEntryPointConsumer
{
    /// <summary>
    /// Subscribes an observer, which will be invoked when a new <see cref="ICompilerServiceProvider"/> is registered.
    /// The observer will also be immediately invoked for all currently registered providers.
    /// </summary>
    IDisposable ObserveOnServiceProviderRegistered( ServiceProviderEventHandler observer );

    /// <summary>
    /// Gets the <see cref="ICompilerServiceProvider"/> for a specific project. This method is called by the VSX.
    /// </summary>
    /// <param name="version">Version of Metalama for which the service is required.</param>
    ValueTask GetServiceProviderAsync( Version version, ICompilerServiceProvider?[] result, CancellationToken cancellationToken );

    /// <summary>
    /// Gets all compatible registered service providers.
    /// </summary>
    ICompilerServiceProvider[] GetRegisteredProviders();

    /// <summary>
    /// Subscribe to an event raised when a provider is registered and it has an invalid contract version (which can happen
    /// in pre-release versions only). The VSX can handle this event and display an error message.
    /// </summary>
    IDisposable ObserveOnContractVersionMismatchDetected( ServiceProviderEventHandler observer );
}