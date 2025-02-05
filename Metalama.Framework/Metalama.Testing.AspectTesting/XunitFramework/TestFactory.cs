// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Infrastructure;
using Metalama.Framework.Engine.Services;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Metalama.Testing.AspectTesting.XunitFramework
{
    internal sealed class TestFactory
    {
        public GlobalServiceProvider ServiceProvider { get; }

        public TestProjectProperties ProjectProperties { get; }

        public TestInput.Factory TestInputFactory { get; }

        public IFileSystem FileSystem { get; }

        public string ProjectName { get; }

        private readonly ConcurrentDictionary<string, TestMethod> _methods = new();
        private readonly ConcurrentDictionary<string, TestClass> _types = new();

        public TestDirectoryOptionsReader DirectoryOptionsReader { get; }

        public TestFactory( GlobalServiceProvider serviceProvider, TestProjectProperties projectProperties, string directory, string assemblyName )
            : this( serviceProvider, projectProperties, new TestDirectoryOptionsReader( serviceProvider, directory ), LoadAssembly( assemblyName ) ) { }

        private static ReflectionAssemblyInfo LoadAssembly( string assemblyName )
        {
            var assembly = Assembly.Load( assemblyName );

            return new ReflectionAssemblyInfo( assembly );
        }

        public TestFactory(
            GlobalServiceProvider serviceProvider,
            TestProjectProperties projectProperties,
            TestDirectoryOptionsReader directoryOptionsReader,
            IAssemblyInfo assemblyInfo )
        {
            this.DirectoryOptionsReader = directoryOptionsReader;

            this.ProjectProperties = projectProperties;
            this.ProjectName = Path.GetFileName( this.ProjectProperties.SourceDirectory );
            this.TestAssembly = new TestAssembly( this );
            this.TestCollection = new TestCollection( this.TestAssembly );
            this.AssemblyInfo = assemblyInfo;
            this.ServiceProvider = serviceProvider;
            this.TestInputFactory = new TestInput.Factory( serviceProvider );
            this.FileSystem = serviceProvider.GetRequiredBackstageService<IFileSystem>();
        }

        public TestMethod GetTestMethod( string relativePath ) => this._methods.GetOrAdd( relativePath, static ( p, me ) => new TestMethod( me, p ), this );

        public TestClass GetTestType( string? relativePath ) => this._types.GetOrAdd( relativePath ?? "", static ( p, me ) => new TestClass( me, p ), this );

        public TestCollection TestCollection { get; }

        public TestAssembly TestAssembly { get; }

        public IAssemblyInfo AssemblyInfo { get; }
    }
}