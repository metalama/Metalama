// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Engine.Services;
using Metalama.Testing.AspectTesting.XunitFramework;
using Metalama.Testing.UnitTesting;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Xunit;
using Xunit.v3;

namespace Metalama.Testing.AspectTesting
{
    /// <summary>
    /// The xunit test framework that turns every file of a test project into a test. It is registered in the test project
    /// by the <c>Xunit.TestFrameworkAttribute</c> that the build of <c>Metalama.Testing.AspectTesting</c> adds.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public sealed class AspectTestFramework : TestFramework
    {
        internal const string DisplayName = "Metalama";

        static AspectTestFramework()
        {
            TestingServices.Initialize();
        }

        private readonly GlobalServiceProvider _serviceProvider;
        private readonly Action<string>? _trace;

        /// <summary>
        /// Initializes a new instance of the <see cref="AspectTestFramework"/> class. This constructor is called by xunit.
        /// </summary>
        [UsedImplicitly]
        public AspectTestFramework()
        {
            // We disable logging by default because it creates too many log records.
            if ( !string.IsNullOrEmpty( Environment.GetEnvironmentVariable( "LogMetalamaTestFramework" ) ) )
            {
                this._trace = message => TestContext.Current.SendDiagnosticMessage( message );
            }

            const string debugEnvironmentVariable = "DebugMetalamaTestFramework";

            if ( !string.IsNullOrEmpty( Environment.GetEnvironmentVariable( debugEnvironmentVariable ) ) )
            {
                this._trace?.Invoke( $"Environment variable '{debugEnvironmentVariable}' detected. Attaching debugger." );
                Debugger.Launch();
            }

            this._serviceProvider = TestFrameworkServiceFactoryProvider.GetServiceProvider();
        }

        /// <inheritdoc />
        public override string TestFrameworkDisplayName => DisplayName;

        /// <inheritdoc />
        protected override ITestFrameworkDiscoverer CreateDiscoverer( Assembly assembly )
            => new TestDiscoverer( TestFactory.GetInstance( this._serviceProvider, assembly ), this._trace );

        /// <inheritdoc />
        protected override ITestFrameworkExecutor CreateExecutor( Assembly assembly )
            => new TestExecutor( this._serviceProvider, TestFactory.GetInstance( this._serviceProvider, assembly ) );
    }
}
