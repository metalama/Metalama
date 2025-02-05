// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Engine.Services;
using Metalama.Testing.AspectTesting.XunitFramework;
using Metalama.Testing.UnitTesting;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Xunit.Abstractions;

namespace Metalama.Testing.AspectTesting
{
    [ExcludeFromCodeCoverage]
    public sealed class AspectTestFramework : ITestFramework, ISourceInformationProvider
    {
        static AspectTestFramework()
        {
            TestingServices.Initialize();
        }

        private readonly GlobalServiceProvider _serviceProvider;
        private readonly IMessageSink? _messageSink;

        // This is the constructor used by the test host process.
        [UsedImplicitly]
        public AspectTestFramework( IMessageSink messageSink )
        {
            // We disable logging by default because it creates too many log records.
            var messageSinkOrNull = string.IsNullOrEmpty( Environment.GetEnvironmentVariable( "LogMetalamaTestFramework" ) )
                ? null
                : messageSink;

            const string debugEnvironmentVariable = "DebugMetalamaTestFramework";

            if ( !string.IsNullOrEmpty( Environment.GetEnvironmentVariable( debugEnvironmentVariable ) ) )
            {
                messageSinkOrNull?.Trace( $"Environment variable '{debugEnvironmentVariable}' detected. Attaching debugger." );
                Debugger.Launch();
            }

            this._serviceProvider = TestFrameworkServiceFactoryProvider.GetServiceProvider();
        }

        internal AspectTestFramework( GlobalServiceProvider serviceProvider, IMessageSink? messageSink )
        {
            this._serviceProvider = serviceProvider;
            this._messageSink = messageSink;
        }

        void IDisposable.Dispose() { }

        ISourceInformation ISourceInformationProvider.GetSourceInformation( ITestCase testCase ) => (ISourceInformation) testCase;

        ITestFrameworkDiscoverer ITestFramework.GetDiscoverer( IAssemblyInfo assembly )
            => new TestDiscoverer( this._serviceProvider, assembly, this._messageSink );

        ITestFrameworkExecutor ITestFramework.GetExecutor( AssemblyName assemblyName ) => new TestExecutor( this._serviceProvider, assemblyName );

        ISourceInformationProvider ITestFramework.SourceInformationProvider
        {
            set { }
        }
    }
}