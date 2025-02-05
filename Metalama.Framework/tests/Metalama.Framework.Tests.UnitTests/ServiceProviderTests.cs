// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Maintenance;
using Metalama.Framework.DesignTime.Pipeline.Diff;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Project;
using Metalama.Framework.Services;
using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Testing.UnitTesting;
using System;
using System.Threading.Tasks;
using Xunit;

#pragma warning disable VSTHRD200

namespace Metalama.Framework.Tests.UnitTests
{
    public sealed class ServiceProviderTests : UnitTestClass
    {
        // It may seem redundant to test both IGlobalService and IProjectService, but there was a bug where the interface name was hardcoded
        // instead of the generic parameter.

        [Fact]
        public void GlobalService_NextProvider()
        {
            var serviceProvider1 = ServiceProvider<IGlobalService>.Empty.WithService( new TestGlobalService() );
            Assert.NotNull( serviceProvider1.GetService<TestGlobalService>() );

            var serviceProvider2 = ServiceProvider<IGlobalService>.Empty.WithNextProvider( serviceProvider1 );
            Assert.NotNull( serviceProvider2.GetService<TestGlobalService>() );
        }

        [Fact]
        public void GlobalService_AccessByInterface()
        {
            var serviceProvider1 = ServiceProvider<IGlobalService>.Empty.WithService( new TestGlobalService() );
            Assert.NotNull( serviceProvider1.GetService<TestGlobalService>() );
            Assert.NotNull( serviceProvider1.GetService<ITestGlobalService>() );
        }

        [Fact]
        public void GlobalService_NextProvider_AccessByInterface()
        {
            var serviceProvider1 = ServiceProvider<IGlobalService>.Empty.WithService( new TestGlobalService() );
            Assert.NotNull( serviceProvider1.GetService<TestGlobalService>() );
            Assert.NotNull( serviceProvider1.GetService<ITestGlobalService>() );
            var serviceProvider2 = ServiceProvider<IGlobalService>.Empty.WithNextProvider( serviceProvider1 );
            Assert.NotNull( serviceProvider2.GetService<ITestGlobalService>() );
        }

        [Fact]
        public void ProjectService_NextProvider()
        {
            var serviceProvider1 = ServiceProvider<IProjectService>.Empty.WithService( new TestProjectService() );
            Assert.NotNull( serviceProvider1.GetService<TestProjectService>() );

            var serviceProvider2 = ServiceProvider<IProjectService>.Empty.WithNextProvider( serviceProvider1 );
            Assert.NotNull( serviceProvider2.GetService<TestProjectService>() );
        }

        [Fact]
        public void ProjectService_AccessByInterface()
        {
            var serviceProvider1 = ServiceProvider<IProjectService>.Empty.WithService( new TestProjectService() );
            Assert.NotNull( serviceProvider1.GetService<TestProjectService>() );
            Assert.NotNull( serviceProvider1.GetService<ITestProjectService>() );
        }

        [Fact]
        public void GlobalProject_NextProvider_AccessByInterface()
        {
            var serviceProvider1 = ServiceProvider<IProjectService>.Empty.WithService( new TestProjectService() );
            Assert.NotNull( serviceProvider1.GetService<TestProjectService>() );
            Assert.NotNull( serviceProvider1.GetService<ITestProjectService>() );
            var serviceProvider2 = ServiceProvider<IProjectService>.Empty.WithNextProvider( serviceProvider1 );
            Assert.NotNull( serviceProvider2.GetService<ITestProjectService>() );
        }

        [Fact]
        public async Task AsyncConfigurationWithNextProvider()
        {
            Assert.Null( ServiceProviderFactory.AsyncLocalConfiguration );

            var backstageServiceProvider = new TestServiceProvider();

            ServiceProviderFactory.AsyncLocalConfiguration = new ServiceProviderFactoryConfiguration
            {
                NextProvider = backstageServiceProvider, AdditionalServices = new AdditionalServiceCollection( new TestGlobalService() )
            };

            Assert.Same( backstageServiceProvider, ServiceProviderFactory.GetServiceProvider().GetRequiredBackstageService<ITempFileManager>() );

            await Task.Yield();

            Assert.Same( backstageServiceProvider, ServiceProviderFactory.GetServiceProvider().GetRequiredBackstageService<ITempFileManager>() );

            _ = ServiceProviderFactory.GetServiceProvider().GetRequiredService<TestGlobalService>();
        }

        [Fact]
        public async Task AsyncConfigurationWithoutNextProvider()
        {
            Assert.Null( ServiceProviderFactory.AsyncLocalConfiguration );

            ServiceProviderFactory.AsyncLocalConfiguration = new ServiceProviderFactoryConfiguration
            {
                AdditionalServices = new AdditionalServiceCollection( new TestGlobalService() )
            };

            _ = ServiceProviderFactory.GetServiceProvider().GetRequiredBackstageService<ITempFileManager>();

            await Task.Yield();

            _ = ServiceProviderFactory.GetServiceProvider().GetRequiredBackstageService<ITempFileManager>();
        }

        [Fact]
        public void Observer()
        {
            var observer = new DifferObserver();
            var mocks = new AdditionalServiceCollection( observer );
            using var testContext = this.CreateTestContext( mocks );
            Assert.Same( observer, testContext.ServiceProvider.Global.GetService<IDifferObserver>() );
        }

        private interface ITestGlobalService : IGlobalService;

        private interface ITestProjectService : IProjectService;

        private sealed class TestGlobalService : ITestGlobalService;

        private sealed class TestProjectService : ITestProjectService;

        private sealed class TestServiceProvider : IServiceProvider, ITempFileManager
        {
            public object? GetService( Type serviceType )
            {
                if ( serviceType == typeof(ITempFileManager) )
                {
                    return this;
                }
                else
                {
                    return null;
                }
            }

            public string GetTempDirectory( string directory, CleanUpStrategy cleanUpStrategy, string? subdirectory, TempFileVersionScope versionScope )
                => throw new NotImplementedException();
        }
    }
}