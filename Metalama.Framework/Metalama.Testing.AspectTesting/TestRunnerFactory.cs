// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;
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
                    var typeName = testInput.Options.TestRunnerFactoryType!;

                    if ( !typeName.ContainsOrdinal( ',' ) && testInput.ProjectProperties.AssemblyName != null )
                    {
                        typeName = $"{typeName}, {testInput.ProjectProperties.AssemblyName}";
                    }

                    factoryType = Type.GetType( typeName, true )!;
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
    }
}