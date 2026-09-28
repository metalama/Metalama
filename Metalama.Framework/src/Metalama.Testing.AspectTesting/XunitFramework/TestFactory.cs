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
        private static readonly ConcurrentDictionary<Assembly, TestFactory> _instances = new();

        private readonly ConcurrentDictionary<string, TestMethod> _methods = new();
        private readonly ConcurrentDictionary<string, TestClass> _types = new();

        public GlobalServiceProvider ServiceProvider { get; }

        public TestProjectProperties ProjectProperties { get; }

        public TestInput.Factory TestInputFactory { get; }

        public IFileSystem FileSystem { get; }

        public string ProjectName { get; }

        public TestDirectoryOptionsReader DirectoryOptionsReader { get; }

        public Assembly Assembly { get; }

        public TestCollection TestCollection { get; }

        public TestAssembly TestAssembly { get; }

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

        public TestMethod GetTestMethod( string relativePath ) => this._methods.GetOrAdd( relativePath, static ( p, me ) => new TestMethod( me, p ), this );

        public TestClass GetTestType( string? relativePath ) => this._types.GetOrAdd( relativePath ?? "", static ( p, me ) => new TestClass( me, p ), this );

        /// <summary>
        /// Gets the empty dictionary of traits that every test object of this framework reports.
        /// </summary>
        public static IReadOnlyDictionary<string, IReadOnlyCollection<string>> EmptyTraits { get; } =
            new Dictionary<string, IReadOnlyCollection<string>>( StringComparer.Ordinal );
    }
}
