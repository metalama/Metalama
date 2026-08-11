// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Framework.Engine.Services;
using Metalama.Testing.UnitTesting;
using System;
using Xunit.Abstractions;

namespace Metalama.Testing.AspectTesting
{
    /// <summary>
    /// Instantiates a specific implementation of the  <see cref="BaseTestRunner"/> class.
    /// </summary>
    internal static class TestRunnerFactory
    {
        public static BaseTestRunner CreateTestRunner(
            TestInput testInput,
            GlobalServiceProvider serviceProvider,
            TestProjectReferences references,
            ITestOutputHelper? logger )
        {
            if ( logger != null && testInput.Options.EnableLogging.GetValueOrDefault() )
            {
                serviceProvider = serviceProvider.Underlying.WithUntypedService( typeof(ILoggerFactory), new XunitLoggerFactory( logger ) );
            }

            // Create the ITestRunnerFactory.
            ITestRunnerFactory testRunnerFactory;

            if ( !string.IsNullOrEmpty( testInput.Options.TestRunnerFactoryType ) )
            {
                Type? factoryType;

                try
                {
                    factoryType = ResolveTestRunnerFactoryType( testInput.Options.TestRunnerFactoryType!, testInput.ProjectProperties.AssemblyName );
                }
                catch ( Exception e )
                {
                    throw new InvalidOperationException( $"Cannot instantiate the type '{testInput.Options.TestRunnerFactoryType}': {e.Message}" );
                }

                testRunnerFactory = (ITestRunnerFactory) Activator.CreateInstance( factoryType )!;
            }
            else
            {
                switch ( testInput.Options.TestScenario )
                {
                    case TestScenario.DesignTime:
                        testRunnerFactory = new DesignTimeTestRunnerFactory();

                        break;

                    case TestScenario.Preview:
                        testRunnerFactory = new PreviewTestRunnerFactory();

                        break;

                    case TestScenario.LiveTemplatePreview:
                    case TestScenario.LiveTemplate:
                        testRunnerFactory = new LiveTemplateTestRunnerFactory();

                        break;

                    default:
                        testRunnerFactory = new AspectTestRunnerFactory();

                        break;
                }
            }

            return testRunnerFactory.CreateTestRunner(
                serviceProvider,
                testInput.ProjectDirectory,
                references,
                logger );
        }

        /// <summary>
        /// Resolves the type named by the <c>TestRunnerFactoryType</c> option.
        /// </summary>
        /// <remarks>
        /// The assembly qualification of the name is treated as a hint rather than as part of the identity, because the
        /// assemblies it can name are built once per supported Roslyn version and carry the version in their name. A
        /// <c>metalamaTests.json</c> naming <c>Metalama.Testing.AspectTesting</c> therefore resolves nothing in the
        /// project compiled against the older Roslyn, whose assembly is <c>Metalama.Testing.AspectTesting.4.12.0</c>.
        /// Writing the version into the payload files is not an option, because the same payload is shared by every
        /// variant, so the name is resolved against the assemblies actually running instead.
        /// </remarks>
        private static Type ResolveTestRunnerFactoryType( string typeName, string? testAssemblyName )
        {
            var commaIndex = typeName.IndexOf( ",", StringComparison.Ordinal );
            var unqualifiedTypeName = commaIndex < 0 ? typeName : typeName.Substring( 0, commaIndex ).Trim();

            if ( commaIndex >= 0 )
            {
                var type = Type.GetType( typeName, false );

                if ( type != null )
                {
                    return type;
                }
            }

            // The factory lives either in this assembly, which is the case of every factory the framework provides, or
            // in the test assembly itself.
            return typeof(TestRunnerFactory).Assembly.GetType( unqualifiedTypeName, false )
                   ?? (testAssemblyName == null
                       ? null
                       : Type.GetType( $"{unqualifiedTypeName}, {testAssemblyName}", false ))
                   ?? throw new InvalidOperationException( $"The type '{unqualifiedTypeName}' was not found in any of the running test assemblies." );
        }
    }
}