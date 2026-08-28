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
        /// The assembly name in the type name is used as a hint and not as part of the identity of the type. The
        /// assemblies that it can designate are compiled once per supported Roslyn version, and their name contains
        /// that version. A <c>metalamaTests.json</c> file that names <c>Metalama.Testing.AspectTesting</c> therefore
        /// designates no assembly in the test project compiled against Roslyn 4.12.0, whose assembly is named
        /// <c>Metalama.Testing.AspectTesting.4.12.0</c>. The version cannot be written in the test payload files,
        /// because the same files are compiled by every variant of the test project. The type name is therefore
        /// resolved against the assemblies that are loaded.
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