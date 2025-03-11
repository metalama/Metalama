// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Infrastructure;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Caching;
using Metalama.Testing.AspectTesting.Utilities;
using Metalama.Testing.AspectTesting.XunitFramework;
using Metalama.Testing.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Metalama.Testing.AspectTesting;

/// <summary>
/// A base class for test classes built using the current framework.
/// All test methods must be annotated with both <c>[Theory]</c> and <see cref="CurrentDirectoryAttribute"/>,
/// must have a single parameter accepting the relative path of the test file, and must call <see cref="RunTestAsync"/> as their only implementation.
/// </summary>
public abstract class AspectTestClass
{
    static AspectTestClass()
    {
        TestingServices.Initialize();
    }

    private readonly ITestOutputHelper _logger;
    private readonly GlobalServiceProvider _serviceProvider;
    private readonly ITestAssemblyMetadataReader _metadataReader;

    private sealed record AssemblyAssets(
        TestProjectProperties ProjectProperties,
        TestDirectoryOptionsReader OptionsReader );

    private static readonly WeakCache<Assembly, AssemblyAssets> _cache = new();
    private readonly IFileSystem _fileSystem;

    private static AssemblyAssets GetAssemblyAssets( GlobalServiceProvider serviceProvider, Assembly assembly )
        => _cache.GetOrAdd(
            assembly,
            a =>
            {
                var assemblyInfo = new ReflectionAssemblyInfo( a );
                var discoverer = new TestDiscoverer( serviceProvider, assemblyInfo );

                var projectProperties = discoverer.GetTestProjectProperties();

                return new AssemblyAssets(
                    projectProperties,
                    new TestDirectoryOptionsReader( serviceProvider, projectProperties.SourceDirectory ) );
            } );

    protected AspectTestClass( ITestOutputHelper logger )
    {
        this._logger = logger;
        this._metadataReader = new TestAssemblyMetadataReader();

        this._serviceProvider = ServiceProviderFactory.GetServiceProvider()
            .WithUntypedService( typeof(ILoggerFactory), new XunitLoggerFactory( logger, false ) )
            .WithService( this._metadataReader );

        this._fileSystem = this._serviceProvider.GetRequiredBackstageService<IFileSystem>();
    }

    protected virtual string GetDirectory( string callerMemberName )
    {
        var callerMethod = this
            .GetType()
            .GetMethods( BindingFlags.Instance | BindingFlags.Public )
            .Single( m => m.Name == callerMemberName );

        var testFilesAttribute = callerMethod.GetCustomAttribute<CurrentDirectoryAttribute>()
                                 ??
                                 throw new InvalidOperationException( "The calling method does not have a [TestFiles] attribute." );

        return testFilesAttribute.Directory;
    }

    /// <summary>
    /// Executes a test.
    /// </summary>
    /// <param name="relativePath">Relative path of the file relatively to the directory of the caller code.</param>
    [PublicAPI]
    protected async Task RunTestAsync( string relativePath, [CallerMemberName] string? callerMemberName = null )
    {
        var testSuiteAssembly = this.GetType().Assembly;
        var assemblyAssets = GetAssemblyAssets( this._serviceProvider, testSuiteAssembly );
        var projectReferences = this._metadataReader.GetMetadata( new ReflectionAssemblyInfo( this.GetType().Assembly ) ).ToProjectReferences();

        var directory = this.GetDirectory( callerMemberName! );

        var fullPath = Path.Combine( directory, relativePath );

        this._logger.WriteLine( "Test input file: " + fullPath );
        var projectRelativePath = this._fileSystem.GetRelativePath( assemblyAssets.ProjectProperties.SourceDirectory, fullPath );

        var testInputFactory = new TestInput.Factory();
        var testInput = testInputFactory.FromFile( assemblyAssets.ProjectProperties, assemblyAssets.OptionsReader, projectRelativePath );

        var testOptions =
            testInput.Options.ApplyToTestContextOptions( new TestContextOptions { AdditionalMetadataReferences = projectReferences.MetadataReferences } );

        var testRunner = TestRunnerFactory.CreateTestRunner( testInput, this._serviceProvider, projectReferences, this._logger );
        await testRunner.RunAndAssertAsync( testInput, testOptions );
    }
}