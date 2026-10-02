// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Templating;
using Metalama.Framework.Tests.TemplateTests.Runner;
using Metalama.Testing.AspectTesting;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.TestFramework
{
    /// <summary>
    /// Tests the <see cref="TemplatingTestRunner"/> class, which is compiled into this project from the template test project.
    /// </summary>
    public sealed class TemplatingTestRunnerTests : UnitTestClass
    {
        private const string _templateTestSource = """
                                                   using Metalama.Framework.Aspects;
                                                   using Metalama.Framework.Engine.Templating;

                                                   internal class Aspect
                                                   {
                                                       [TestTemplate]
                                                       private dynamic? Template() => meta.Proceed();
                                                   }

                                                   internal class TargetCode
                                                   {
                                                       private int Method( int a ) => a;
                                                   }
                                                   """;

#if NET5_0_OR_GREATER
        private const string _targetFramework = "net8.0";
#else
        private const string _targetFramework = "net48";
#endif

        private readonly ITestOutputHelper _logger;

        public TemplatingTestRunnerTests( ITestOutputHelper logger ) : base( logger )
        {
            this._logger = logger;
        }

        /// <summary>
        /// Verifies that two template tests with the same file name in different directories write their compile-time code
        /// to different files, so that they do not conflict when xUnit runs them in parallel.
        /// </summary>
        [Fact]
        public async Task TestsWithSameFileNameWriteToDifferentCompileTimeFiles()
        {
            var contextOptions = new MetalamaTestContextOptions
            {
                AdditionalMetadataReferences = [MetadataReference.CreateFromFile( typeof(TestTemplateAttribute).Assembly.Location )]
            };

            using var testContext = new MetalamaTestContext( contextOptions );

            var firstPath = await this.RunAndGetCompileTimePathAsync( testContext, Path.Combine( "First", "SameName.cs" ) );
            var secondPath = await this.RunAndGetCompileTimePathAsync( testContext, Path.Combine( "Second", "SameName.cs" ) );

            Assert.NotEqual( Path.GetFullPath( firstPath ), Path.GetFullPath( secondPath ) );

            var generatedDirectory = Path.Combine( testContext.BaseDirectory, "obj", _targetFramework, "generated" );
            Assert.Equal( Path.Combine( generatedDirectory, "First", "SameName.cs" ), firstPath );
            Assert.Equal( Path.Combine( generatedDirectory, "Second", "SameName.cs" ), secondPath );
        }

        private async Task<string> RunAndGetCompileTimePathAsync( MetalamaTestContext testContext, string relativePath )
        {
            var projectDirectory = testContext.BaseDirectory;
            var fullPath = Path.Combine( projectDirectory, relativePath );
            Directory.CreateDirectory( Path.GetDirectoryName( fullPath )! );
            File.WriteAllText( fullPath, _templateTestSource );

            var serviceProvider = (GlobalServiceProvider) testContext.ServiceProvider.Global.Underlying;

            var projectProperties = new TestProjectProperties(
                assemblyName: null,
                projectDirectory,
                projectDirectory,
                ImmutableArray<string>.Empty,
                _targetFramework,
                _targetFramework,
                ImmutableArray<string>.Empty );

            var directoryOptionsReader = new TestDirectoryOptionsReader( serviceProvider, projectDirectory );
            var testInput = new TestInput.Factory( serviceProvider ).FromFile( projectProperties, directoryOptionsReader, relativePath );

            var references = new TestProjectReferences(
                testContext.GetMetadataReferences().ToImmutableArray(),
                ImmutableArray<TargetedAssemblyReference>.Empty,
                ImmutableArray<TargetedAssemblyReference>.Empty,
                ImmutableArray<string>.Empty,
                null );

            var testRunner = new TemplatingTestRunner( serviceProvider, projectDirectory, references, this._logger );
            var testResult = await testRunner.RunAsync( testInput, testContext );

            Assert.True( testResult.Success, testResult.ErrorMessage );

            var compileTimePath = testResult.SyntaxTrees.Single().OutputCompileTimePath;
            Assert.NotNull( compileTimePath );

            return compileTimePath;
        }
    }
}
