// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable InconsistentlySynchronizedField

namespace Metalama.Framework.DesignTime.Contracts.EntryPoint;

public sealed partial class DesignTimeEntryPointManager
{
    private sealed class Consumer : IDesignTimeEntryPointConsumer
    {
        private readonly DesignTimeEntryPointManager _parent;
        private readonly ImmutableDictionary<string, int> _contractVersions;
        private readonly ConcurrentDictionary<Version, Task<ICompilerServiceProvider>> _getProviderTasks = new();

        public Consumer( DesignTimeEntryPointManager parent, ImmutableDictionary<string, int> contractVersions )
        {
            this._parent = parent;
            this._contractVersions = contractVersions;
        }

        private bool ValidateContractVersions( ContractVersion[] candidates )
        {
            foreach ( var supportedVersion in this._contractVersions )
            {
                var candidateVersion = candidates.SingleOrDefault( c => c.Version == supportedVersion.Key ).Revision;

                if ( candidateVersion != 0 && candidateVersion != supportedVersion.Value )
                {
                    return false;
                }
            }

            return true;
        }

        public async ValueTask GetServiceProviderAsync( Version version, ICompilerServiceProvider?[] result, CancellationToken cancellationToken )
        {
            var task = this._getProviderTasks.GetOrAdd( version, this.GetProviderForVersionAsync );

            if ( !task.IsCompleted )
            {
                var taskCancelled = new TaskCompletionSource<bool>();

#if NET5_0_OR_GREATER
                await using ( cancellationToken.Register( () => taskCancelled.SetCanceled( cancellationToken ) ) )
#else
                using ( cancellationToken.Register( () => taskCancelled.SetCanceled() ) )
#endif
                {
                    await Task.WhenAny( task, taskCancelled.Task );

                    if ( taskCancelled.Task.IsCanceled )
                    {
                        throw new TaskCanceledException();
                    }
                }
            }

            result[0] = await task;
        }

        // ReSharper disable once InconsistentlySynchronizedField
        public ICompilerServiceProvider[] GetRegisteredProviders() => this._parent._providers.ToArray();

        public IDisposable ObserveOnContractVersionMismatchDetected( ServiceProviderEventHandler observer )
        {
            this.ContractVersionMismatchDetected += observer;

            return new DisposeAction( () => this.ContractVersionMismatchDetected -= observer );
        }

        public event ServiceProviderEventHandler? ContractVersionMismatchDetected;

        private async Task<ICompilerServiceProvider> GetProviderForVersionAsync( Version version )
        {
            this._parent._logger?.Invoke( $"GetProviderForVersionAsync({version})" );

            while ( true )
            {
                lock ( this._parent._sync )
                {
                    foreach ( var entryPoint in this._parent._providers )
                    {
                        if ( entryPoint.Version == version )
                        {
                            if ( this.ValidateContractVersions( entryPoint.ContractVersions ) )
                            {
                                this._parent._logger?.Invoke(
                                    $"GetProviderForVersionAsync({version}): found a valid version with matching contract versions." );

                                return entryPoint;
                            }
                            else
                            {
                                this._parent._logger?.Invoke(
                                    $"GetProviderForVersionAsync({version}): found a valid version, but contract versions do not match." );

                                this.ContractVersionMismatchDetected?.Invoke( entryPoint );

                                return new InvalidCompilerServiceProvider( entryPoint.Version, entryPoint.ContractVersions );
                            }
                        }
                    }
                }

                this._parent._logger?.Invoke( $"GetProviderForVersionAsync({version}): waiting for a new registration." );

                await this._parent._registrationTask.Task;
            }
        }

        public IDisposable ObserveOnServiceProviderRegistered( ServiceProviderEventHandler observer )
        {
            lock ( this._parent._sync )
            {
                foreach ( var provider in this._parent._providers )
                {
                    observer.Invoke( provider );
                }

                var observerId = this._parent._nextObserverId++;
                this._parent._observers = this._parent._observers.Add( observerId, observer );

                return new DisposeAction(
                    () =>
                    {
                        lock ( this._parent._sync )
                        {
                            this._parent._observers = this._parent._observers.Remove( observerId );
                        }
                    } );
            }
        }
    }
}