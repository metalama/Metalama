// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Infrastructure;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;
using Metalama.Testing.AspectTesting.Utilities;
using Metalama.Testing.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Metalama.Testing.AspectTesting.XunitFramework
{
    [Serializable]
    internal class TestDiscoverer : LongLivedMarshalByRefObject, ITestFrameworkDiscoverer
    {
        static TestDiscoverer()
        {
            TestingServices.Initialize();
        }

        private static readonly HashSet<string> _excludedDirectoryNames = new( StringComparer.OrdinalIgnoreCase ) { "bin", "obj" };
        private readonly IAssemblyInfo _assembly;
        private readonly IMessageSink? _messageSink;
        private readonly GlobalServiceProvider _serviceProvider;

        public IFileSystem FileSystem { get; }

        private readonly ITestAssemblyMetadataReader _metadataReader;

        public TestDiscoverer( IAssemblyInfo assembly, IMessageSink? messageSink = null ) : this(
            ServiceProviderFactory.GetServiceProvider().WithService( new TestAssemblyMetadataReader() ),
            assembly,
            messageSink ) { }

        public TestDiscoverer( GlobalServiceProvider serviceProvider, IAssemblyInfo assembly, IMessageSink? messageSink = null )
        {
            this._assembly = assembly;
            this._messageSink = messageSink;
            this._serviceProvider = serviceProvider;
            this.FileSystem = serviceProvider.GetRequiredBackstageService<IFileSystem>();
            this._metadataReader = serviceProvider.GetRequiredService<ITestAssemblyMetadataReader>();

            var attribute = assembly.GetCustomAttributes( typeof(TargetFrameworkAttribute) ).FirstOrDefault();

            if ( attribute != null )
            {
                this.TargetFramework = attribute.GetConstructorArguments().Cast<string>().First();
            }
            else
            {
                this.TargetFramework = null!;
            }
        }

        void IDisposable.Dispose() { }

        public TestProjectProperties GetTestProjectProperties()
        {
            var metadata = this._metadataReader.GetMetadata( this._assembly );

            return new TestProjectProperties(
                this._assembly.Name,
                metadata.ProjectDirectory,
                metadata.SourceDirectory,
                metadata.ParserSymbols,
                metadata.TargetFramework,
                metadata.IgnoredWarnings );
        }

        public List<TestCase> Discover( string subDirectory, ImmutableHashSet<string> excludedDirectories )
        {
            List<TestCase> testCases = new();
            this.Discover( c => testCases.Add( c ), subDirectory, false, excludedDirectories );

            return testCases;
        }

        private void Discover(
            Action<TestCase> onTestCaseDiscovered,
            string? subDirectory,
            bool isXUnitFrameworkDiscovery,
            ImmutableHashSet<string> excludedSubdirectories )
        {
            var sync = new object();

            this._messageSink?.Trace( $"Discovering tests in directory '{subDirectory}'." );

            var projectProperties = this.GetTestProjectProperties();
            TestDirectoryOptionsReader reader = new( this._serviceProvider, projectProperties.SourceDirectory );
            TestFactory factory = new( this._serviceProvider, projectProperties, reader, this._assembly );

            ConcurrentQueue<Task> tasks = new();
            var pendingTasks = new StrongBox<int>( 0 );

            void AddTestsInDirectory( string directory )
            {
                try
                {
                    // Skip bin, obj.
                    if ( _excludedDirectoryNames.Contains( Path.GetFileName( directory ) ) )
                    {
                        return;
                    }

                    var options = reader.GetDirectoryOptions( directory );

                    // If the directory is excluded, don't continue.
                    if ( options.Exclude.GetValueOrDefault() )
                    {
                        this._messageSink?.Trace( $"Child directory '{directory}' excluded because of the Exclude option." );

                        return;
                    }

                    this._messageSink?.Trace( $"Processing directory '{directory}'." );

                    // If the directory is included, index the files.
                    const string runnerFileName = "_Runner.cs";

                    foreach ( var testPath in this.FileSystem.EnumerateFiles( directory, "*.cs" ) )
                    {
                        var fileName = Path.GetFileName( testPath );

                        if ( fileName[0] == '_' )
                        {
                            continue;
                        }

                        var firstDotPosition = fileName.IndexOfOrdinal( '.' );
                        var extension = fileName.Substring( firstDotPosition );

                        if ( !string.Equals( extension, ".cs", StringComparison.Ordinal ) )
                        {
                            // Skipping.

                            continue;
                        }

                        if ( Path.GetFileName( testPath ).Equals( runnerFileName, StringComparison.OrdinalIgnoreCase ) )
                        {
                            continue;
                        }

                        this._messageSink?.Trace( $"Including the file '{testPath}'" );

                        var testCase = new TestCase( factory, this.FileSystem.GetRelativePath( projectProperties.SourceDirectory, testPath ) );

                        this._messageSink?.Trace(
                            $"    {((ITestCase) testCase).TestMethod.TestClass.TestCollection.TestAssembly.Assembly.Name} " +
                            $"/ {((ITestCase) testCase).TestMethod.TestClass.TestCollection.DisplayName}" +
                            $"/ {((ITestCase) testCase).TestMethod.TestClass.Class.Name} " +
                            $"/ {((ITestCase) testCase).TestMethod.Method.Name} " +
                            $"/ {((ITestCase) testCase).DisplayName}" );

                        // Somehow xunit does not like if we call onTestCaseDiscovered concurrently.
                        lock ( sync )
                        {
                            onTestCaseDiscovered( testCase );
                        }
                    }

                    // Process children directories.

                    foreach ( var nestedDir in this.FileSystem.EnumerateDirectories( directory ) )
                    {
                        if ( excludedSubdirectories.Contains( nestedDir ) )
                        {
                            this._messageSink?.Trace( $"Child directory '{nestedDir}' excluded because it is covered by other tests." );

                            continue;
                        }

                        if ( !isXUnitFrameworkDiscovery )
                        {
                            // Don't include a directory that has a _Runner file.
                            var runnerFile = Path.Combine( nestedDir, runnerFileName );

                            if ( File.Exists( runnerFile ) )
                            {
                                this._messageSink?.Trace( $"Child directory '{nestedDir}' excluded it contains '{runnerFileName}'." );

                                continue;
                            }
                        }

                        Interlocked.Increment( ref pendingTasks.Value );
                        tasks.Enqueue( Task.Run( () => AddTestsInDirectory( nestedDir ) ) );
                    }
                }
                finally
                {
                    Interlocked.Decrement( ref pendingTasks.Value );
                }
            }

            Interlocked.Increment( ref pendingTasks.Value );
            AddTestsInDirectory( subDirectory ?? reader.ProjectDirectory );

            while ( pendingTasks.Value > 0 )
            {
                // Waiting synchronously here is safe because the execution context is always a background process,
                // and addressing the warning otherwise is cumbersome.
#pragma warning disable VSTHRD002
                Task.WhenAll( tasks ).Wait();
#pragma warning restore VSTHRD002
            }
        }

        void ITestFrameworkDiscoverer.Find( bool includeSourceInformation, IMessageSink discoveryMessageSink, ITestFrameworkDiscoveryOptions discoveryOptions )
        {
            this.Discover( testCase => discoveryMessageSink.OnMessage( new TestCaseDiscoveryMessage( testCase ) ), null, true, ImmutableHashSet<string>.Empty );
            discoveryMessageSink.OnMessage( new DiscoveryCompleteMessage() );
        }

        void ITestFrameworkDiscoverer.Find(
            string typeName,
            bool includeSourceInformation,
            IMessageSink discoveryMessageSink,
            ITestFrameworkDiscoveryOptions discoveryOptions )
            => throw new NotImplementedException();

        string ITestFrameworkDiscoverer.Serialize( ITestCase testCase ) => testCase.UniqueID;

        public string TargetFramework { get; }

        string ITestFrameworkDiscoverer.TestFrameworkDisplayName => "Metalama";
    }
}