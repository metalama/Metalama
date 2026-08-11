// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Threading;
using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Metalama.Framework.DesignTime.Contracts.EntryPoint
{
    /// <summary>
    /// Exposes a global connection point between compiler assemblies, included in NuGet packages and loaded by Roslyn,
    /// and the UI assemblies, included in the VSX and loaded by Visual Studio. Compiler assemblies register
    /// themselves using <see cref="IDesignTimeEntryPointManager.RegisterServiceProvider"/> and UI assemblies get the
    /// interface using <see cref="IDesignTimeEntryPointManager.GetConsumer"/> and then calling the methods of this interface.
    /// Since VS session can contain projects with several versions of Metalama, this class has the responsibility
    /// to match versions.
    /// </summary>
    public sealed partial class DesignTimeEntryPointManager : IDesignTimeEntryPointManager
    {
        private const string _appDomainDataName = "Metalama.Framework.DesignTime.Contracts.v2.DesignTimeEntryPointManager";

        [ExcludeFromCodeCoverage]
        public static IDesignTimeEntryPointManager Instance { get; }

        [ExcludeFromCodeCoverage]
        static DesignTimeEntryPointManager()
        {
            // Note that there maybe many instances of this class in the AppDomain, so it needs to make sure it uses a shared point of contact.
            // We're using a named AppDomain data slot for this. We have to synchronize access using a named lock.
            //
            // NamedLockService is shared with Metalama.Backstage by compiling the same source files, because this
            // assembly is loaded side by side by every Metalama version present in one Visual Studio session and
            // must stay version-frozen, which forbids referencing anything.
            //
            // The name is used verbatim and must never change: it is what makes the copies of this class that
            // belong to different Metalama versions exclude each other.
            var lockService = new NamedLockService();

            using var entryPointLock = lockService.GetLock( $@"Local\{_appDomainDataName}" );

            // The wait is unbounded, so this cannot fail. A lock abandoned by a process that terminated is
            // acquired normally, which is correct here: the data slot it protects is either set or not.
            using var entryPointLockHandle = entryPointLock.Acquire();

            var untypedSharedInstance = AppDomain.CurrentDomain.GetData( _appDomainDataName );
            var sharedInstance = (IDesignTimeEntryPointManager?) untypedSharedInstance;

            if ( sharedInstance != null )
            {
                Instance = sharedInstance;
            }
            else
            {
                Instance = new DesignTimeEntryPointManager();
                AppDomain.CurrentDomain.SetData( _appDomainDataName, Instance );
            }
        }

        // The constructor is public because it is used for tests, so we don't base tests on the singleton instance.
        // ReSharper disable once EmptyConstructor
        public DesignTimeEntryPointManager() { }

        private readonly object _sync = new();
        private volatile TaskCompletionSource<ICompilerServiceProvider> _registrationTask = new();
        private volatile ImmutableHashSet<ICompilerServiceProvider> _providers = ImmutableHashSet<ICompilerServiceProvider>.Empty;
        private int _nextObserverId;

        private volatile ImmutableDictionary<int, ServiceProviderEventHandler> _observers =
            ImmutableDictionary<int, ServiceProviderEventHandler>.Empty;

        private LogAction? _logger;

        public void SetLogger( LogAction? logger ) => this._logger = logger;

        public IDesignTimeEntryPointConsumer GetConsumer( ContractVersion[] contractVersions )
            => new Consumer( this, contractVersions.ToImmutableDictionary( i => i.Version, i => i.Revision ) );

        public void RegisterServiceProvider( ICompilerServiceProvider entryPoint )
        {
            lock ( this._sync )
            {
                this._providers = this._providers.Add( entryPoint );

                this._logger?.Invoke( $"Registering service provider v{entryPoint.Version}." );

                // The order here is important.
                var oldRegistrationTask = this._registrationTask;
                this._registrationTask = new TaskCompletionSource<ICompilerServiceProvider>();
                oldRegistrationTask.SetResult( entryPoint );

                // Send notifications.
                foreach ( var observer in this._observers )
                {
                    this._logger?.Invoke( $"Notifying observer." );

                    observer.Value.Invoke( entryPoint );
                }
            }
        }

        Version IDesignTimeEntryPointManager.Version => this.GetType().Assembly.GetName().Version!;
    }
}