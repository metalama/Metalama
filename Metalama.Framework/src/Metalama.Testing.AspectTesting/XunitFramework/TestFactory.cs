// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Services;
using SharpCrafters.Backstage.Infrastructure;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Metalama.Testing.AspectTesting.XunitFramework
{
    /// <summary>
    /// Creates and caches the objects that describe the tests of a test assembly: the assembly, the collection, the classes
    /// and the methods.
    /// </summary>
    internal sealed class TestFactory
    {
        /// <summary>
        /// The single instance of the factory of each test assembly.
        /// </summary>
        private static readonly ConcurrentDictionary<Assembly, TestFactory> _instances = new();

        /// <summary>
        /// The test methods, keyed by the relative path of their test file.
        /// </summary>
        private readonly ConcurrentDictionary<string, TestMethod> _methods = new();
        /// <summary>
        /// The test classes, keyed by the relative path of their directory.
        /// </summary>
        private readonly ConcurrentDictionary<string, TestClass> _types = new();

        /// <summary>
        /// Gets the global service provider of the test framework.
        /// </summary>
        public GlobalServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Gets the properties of the test project.
        /// </summary>
        public TestProjectProperties ProjectProperties { get; }

        /// <summary>
        /// Gets the factory that reads test files.
        /// </summary>
        public TestInput.Factory TestInputFactory { get; }

        /// <summary>
        /// Gets the file system.
        /// </summary>
        public IFileSystem FileSystem { get; }

        /// <summary>
        /// Gets the name of the test project, which is the name of its source directory.
        /// </summary>
        public string ProjectName { get; }

        /// <summary>
        /// Gets the reader of the options that apply to the directories of the test project.
        /// </summary>
        public TestDirectoryOptionsReader DirectoryOptionsReader { get; }

        /// <summary>
        /// Gets the test assembly.
        /// </summary>
        public Assembly Assembly { get; }

        /// <summary>
        /// Gets the single test collection of the test assembly.
        /// </summary>
        public TestCollection TestCollection { get; }

        /// <summary>
        /// Gets the object that describes the test assembly to xunit.
        /// </summary>
        public TestAssembly TestAssembly { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestFactory"/> class.
        /// </summary>
        public TestFactory(
            GlobalServiceProvider serviceProvider,
            TestProjectProperties projectProperties,
            TestDirectoryOptionsReader directoryOptionsReader,
            Assembly assembly )
        {
            this.DirectoryOptionsReader = directoryOptionsReader;
            this.ProjectProperties = projectProperties;
            this.ProjectName = Path.GetFileName( this.ProjectProperties.SourceDirectory );
            this.Assembly = assembly;
            this.ServiceProvider = serviceProvider;
            this.TestInputFactory = new TestInput.Factory( serviceProvider );
            this.FileSystem = serviceProvider.GetRequiredBackstageService<IFileSystem>();
            this.TestAssembly = new TestAssembly( this );
            this.TestCollection = new TestCollection( this.TestAssembly );
        }

        /// <summary>
        /// Gets the <see cref="TestFactory"/> of a test assembly. There is a single instance per assembly, so that the
        /// test cases that xunit deserializes and the test cases that the discoverer creates share the same objects.
        /// </summary>
        public static TestFactory GetInstance( GlobalServiceProvider serviceProvider, Assembly assembly )
            => _instances.GetOrAdd( assembly, static ( a, sp ) => Create( sp, a ), serviceProvider );

        /// <summary>
        /// Gets the <see cref="TestFactory"/> of a test assembly, and creates the service provider of a new instance only
        /// when the assembly has no instance yet.
        /// </summary>
        /// <remarks>
        /// xunit deserializes every test case of a run separately, and creating a global service provider is expensive,
        /// so <see cref="TestCase"/> uses this overload.
        /// </remarks>
        public static TestFactory GetInstance( Func<GlobalServiceProvider> getServiceProvider, Assembly assembly )
            => _instances.GetOrAdd( assembly, static ( a, get ) => Create( get(), a ), getServiceProvider );

        /// <summary>
        /// Creates the factory of a test assembly from the metadata of the assembly.
        /// </summary>
        private static TestFactory Create( GlobalServiceProvider serviceProvider, Assembly assembly )
        {
            var metadata = serviceProvider.GetRequiredService<ITestAssemblyMetadataReader>().GetMetadata( assembly );
            var projectProperties = metadata.ToProjectProperties( assembly.GetName().Name );

            return new TestFactory(
                serviceProvider,
                projectProperties,
                new TestDirectoryOptionsReader( serviceProvider, projectProperties.SourceDirectory ),
                assembly );
        }

        /// <summary>
        /// Gets the test method of a test file, and creates it on first use.
        /// </summary>
        public TestMethod GetTestMethod( string relativePath ) => this._methods.GetOrAdd( relativePath, static ( p, me ) => new TestMethod( me, p ), this );

        /// <summary>
        /// Gets the test class of a directory, and creates it on first use.
        /// </summary>
        public TestClass GetTestType( string? relativePath ) => this._types.GetOrAdd( relativePath ?? "", static ( p, me ) => new TestClass( me, p ), this );

        /// <summary>
        /// Gets the empty dictionary of traits that every test object of this framework reports.
        /// </summary>
        public static IReadOnlyDictionary<string, IReadOnlyCollection<string>> EmptyTraits { get; } =
            new Dictionary<string, IReadOnlyCollection<string>>( StringComparer.Ordinal );
    }
}
