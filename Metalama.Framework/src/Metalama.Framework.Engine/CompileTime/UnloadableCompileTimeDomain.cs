// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if NET5_0_OR_GREATER
using Metalama.Backstage.Utilities;
using Metalama.Framework.Engine.Services;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

// Memory leak verification is currently disabled because it systematically fails on Gael's computer,
// although it works on other computers.

#if VERIFY_MEMORY_LEAKS
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.Utilities;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Metalama.Framework.Engine.Utilities.Threading;
#endif

namespace Metalama.Framework.Engine.CompileTime
{
    /// <summary>
    /// An implementation of <see cref="CompileTimeDomain"/> base on <c>AssemblyLoadContext</c> and able to unload
    /// itself.
    /// </summary>
    public sealed class UnloadableCompileTimeDomain : CompileTimeDomain
    {
        private readonly object _disposeLock = new();

        private AssemblyLoadContext? _assemblyLoadContext;
        private volatile bool _disposed;

#if VERIFY_MEMORY_LEAKS
        private static bool _alreadyHasUnloadingTimeout;
        private readonly List<WeakReference> _collectibleAssemblies = new();
        private readonly ITaskRunner _taskRunner;
        private int _isWaitingForDisposal;
        private readonly TaskCompletionSource<bool> _unloadedTask = new();
#endif

        public UnloadableCompileTimeDomain( GlobalServiceProvider serviceProvider ) : base( serviceProvider )
        {
            this._assemblyLoadContext = new AssemblyLoadContext( "Metalama_" + Guid.NewGuid(), isCollectible: true );

#if VERIFY_MEMORY_LEAKS
            CollectibleExecutionContext.RegisterDisposeAction( this.WaitForDisposal );
            this._taskRunner = serviceProvider.GetRequiredService<ITaskRunner>();
#endif
        }

#pragma warning disable CS0067 // Event is never used
        public event Action<string>? UnloadError;
#pragma warning restore CS0067 // Event is never used

        protected override Assembly LoadAssemblyCore( string path, LoadAssemblyOptions options )
        {
            if ( this._disposed )
            {
                throw new ObjectDisposedException( nameof(UnloadableCompileTimeDomain) );
            }

            Assembly assembly;

            try
            {
                // There seems to be several issues with LoadFromAssemblyPath:
                // - Performance issues during tests. Tests seem to be very slow. 
                //   Breaking into the process with the debugger shows of lot of threads are inside LoadFromAssemblyPath.
                // - Possibly file lock issues, where files are not unlocked, even when the AssemblyLoadContext is disposed of.
                // Therefore, we always use LoadFromStream. 
                using var peStream = RetryHelper.Retry( () => File.OpenRead( path ) );
                var pdbPath = Path.ChangeExtension( path, ".pdb" );
                using var pdbStream = File.Exists( pdbPath ) ? RetryHelper.Retry( () => File.OpenRead( pdbPath ) ) : null;

                assembly = this._assemblyLoadContext.AssertNotNull().LoadFromStream( peStream, pdbStream );
            }
            catch ( Exception e )
            {
                throw new FileLoadException( $"Cannot load '{path}': {e.Message}", e );
            }

            if ( !options.IsShared )
            {
                this.AddCollectibleAssembly( assembly );
            }

            return assembly;
        }

        // ReSharper disable once MemberCanBeMadeStatic.Local
        // ReSharper disable once UnusedParameter.Local
#pragma warning disable CA1822
        private void AddCollectibleAssembly( Assembly assembly )
        {
#if VERIFY_MEMORY_LEAKS
            lock ( this._collectibleAssemblies )
            {
                this._collectibleAssemblies.Add( new WeakReference( assembly ) );
            }
#endif
        }
#pragma warning restore CA1822

#if VERIFY_MEMORY_LEAKS
        [ExcludeFromCodeCoverage]
        private void WaitForDisposal() => this._taskRunner.RunSynchronously( this.WaitForDisposalAsync );

        [ExcludeFromCodeCoverage]
        private Task WaitForDisposalAsync()
        {
            if ( !this._disposed )
            {
                throw new InvalidOperationException( "The Dispose method has not been called." );
            }

            return this.WaitForDisposalCoreAsync();
        }

        private async Task WaitForDisposalCoreAsync()
        {
            // Don't wait twice, because this slows down the tests too significantly and we don't know which one is wrong anyway.
            if ( _alreadyHasUnloadingTimeout )
            {
                return;
            }

            if ( Interlocked.CompareExchange( ref this._isWaitingForDisposal, 1, 0 ) != 0 )
            {
                // Another thread has won.
                await this._unloadedTask.Task;

                return;
            }

            try
            {
                var stopwatch = Stopwatch.StartNew();

                while ( true )
                {
                    List<WeakReference> aliveAssemblies;

                    // While waiting for disposal, we need to prevent any other thread from taking a reference to the list of assemblies
                    // loaded in the AppDomain, because such reference would prevent the assembly from being unloaded.
                    lock ( AppDomainUtility.Sync )
                    {
                        lock ( this._collectibleAssemblies )
                        {
                            aliveAssemblies = this._collectibleAssemblies.Where( r => r.IsAlive ).ToMutableList();
                        }
                    }

                    if ( aliveAssemblies.Count == 0 )
                    {
                        this._unloadedTask.SetResult( true );

                        this.Observer?.OnDomainUnloaded( this );

                        return;
                    }

                    await Task.Delay( 100 );

                    GC.Collect();
                    GC.WaitForFullGCComplete();

                    if ( stopwatch.Elapsed.TotalSeconds > 30 )
                    {
                        _alreadyHasUnloadingTimeout = true;

                        var assemblies = string.Join(
                            ",",
                            aliveAssemblies.SelectAsReadOnlyList( r => (Assembly?) r.Target ).WhereNotNull().Select( a => a.GetName().Name ) );

                        // ReSharper disable CommentTypo

                        /* IF YOU ARE HERE BECAUSE YOU ARE DEBUGGING A MEMORY LEAK
                         *
                         *  1. Reproduce the issue by running a single test, to make sure the memory dump does not capture running tests.
                         *
                         *  Using dotMemory:
                         *    - Go to Dominators
                         *    - Filter for `LoaderAllocator`
                         *    - In the left pane, right-click on `LoaderAllocator`, then do "Open objected retained by this domination path"
                         *    -
                         *
                         *  Using WinDbg:
                         *  - You need to use WinDbg and sos.dll.
                         *  - To install sos.dll, do `dotnet tool install --global dotnet-sos`.
                         *  - To know where sos.dll is and how to load it in WinDbg, type `dotnet sos install`.
                         *  - Follow instructions in https://docs.microsoft.com/en-us/dotnet/standard/assembly/unloadability:
                         *      - !dumpheap -type LoaderAllocator
                         *      - !gcroot -all xxxxx
                         *
                         */

                        // ReSharper restore CommentTypo

                        var reason = "The domain could not be unloaded. There are probably dangling references. " +
                                     "The following assemblies are still loaded: " + assemblies + ".";

                        this.UnloadError?.Invoke( reason );

                        throw new InvalidOperationException( reason );
                    }
                }
            }
            catch ( Exception e )
            {
                this._unloadedTask.TrySetException( e );

                throw;
            }
        }
#endif

        protected override void Dispose( bool disposing )
        {
            if ( this._disposed )
            {
                return;
            }

            lock ( this._disposeLock )
            {
                if ( this._disposed )
                {
                    return;
                }

                base.Dispose( disposing );

                this._assemblyLoadContext?.Unload();
                this._assemblyLoadContext = null;

                this._disposed = true;

                // We cannot wait for complete disposal synchronously because the TestResult object, lower in the stack, typically contains
                // a reference to a build-time assembly. So, we need to put the test out of the stack before the domain
                // can be completely disposed.
            }
        }
    }
}
#endif