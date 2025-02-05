// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Contracts.EntryPoint;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.DevEnvEntryPoint
{
    public sealed class DesignTimeEntryPointManagerTests
    {
        [Fact]
        public async Task RegisterBeforeGetAsync()
        {
            IDesignTimeEntryPointManager manager = new DesignTimeEntryPointManager();
            var consumer = manager.GetConsumer( CurrentContractVersions.All );
            var version = new Version( 1, 0 );
            var provider = new FakeProvider( version );
            manager.RegisterServiceProvider( provider );
            Assert.Equal( provider, await consumer.GetServiceProviderAsync( version ) );
        }

        [Fact]
        public async Task GetBeforeRegisterAsync()
        {
            IDesignTimeEntryPointManager manager = new DesignTimeEntryPointManager();
            var consumer = manager.GetConsumer( CurrentContractVersions.All );
            var version = new Version( 1, 0 );
            var getTask = consumer.GetServiceProviderAsync( version );
            Assert.False( getTask.IsCompleted );

            var provider = new FakeProvider( version );
            manager.RegisterServiceProvider( provider );
            Assert.Equal( provider, await getTask );
        }

        [Fact]
        public async Task CancelGetAsync()
        {
            IDesignTimeEntryPointManager manager = new DesignTimeEntryPointManager();
            var consumer = manager.GetConsumer( CurrentContractVersions.All );
            var version = new Version( 1, 0 );
            CancellationTokenSource cancellationTokenSource = new();
            var task = consumer.GetServiceProviderAsync( version, cancellationTokenSource.Token );
            Assert.False( task.IsCompleted );
#if NET5_0_OR_GREATER
            await cancellationTokenSource.CancelAsync();
#else
            cancellationTokenSource.Cancel();
#endif
            await Assert.ThrowsAsync<TaskCanceledException>( async () => await task );
        }

        [Fact]
        public void Version()
        {
            IDesignTimeEntryPointManager manager = new DesignTimeEntryPointManager();
            Assert.NotNull( manager.Version );
        }

        private sealed class FakeProvider : ICompilerServiceProvider
        {
            public FakeProvider( Version version )
            {
                this.Version = version;
            }

            public Version Version { get; }

            public ContractVersion[] ContractVersions => CurrentContractVersions.All;

            public ICompilerService GetService( Type serviceType ) => throw new NotImplementedException();
        }
    }
}