// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities;
using Metalama.Testing.AspectTesting.Utilities;
using Metalama.Testing.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Sdk;
using Xunit.v3;

namespace Metalama.Testing.AspectTesting.XunitFramework
{
    /// <summary>
    /// Discovers the test files of a test project. Every <c>.cs</c> file under the source directory is a test, unless its
    /// name starts with an underscore, it has more than one extension, or its directory is excluded.
    /// </summary>
    internal sealed class TestDiscoverer : ITestFrameworkDiscoverer
    {
        static TestDiscoverer()
        {
            TestingServices.Initialize();
        }

        /// <summary>
        /// The names of the directories that never contain test files.
        /// </summary>
        private static readonly HashSet<string> _excludedDirectoryNames = new( StringComparer.OrdinalIgnoreCase ) { "bin", "obj" };
        /// <summary>
        /// The factory of the test assembly.
        /// </summary>
        private readonly TestFactory _factory;
        /// <summary>
        /// The function that writes diagnostic messages, or <c>null</c>.
        /// </summary>
        private readonly Action<string>? _trace;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestDiscoverer"/> class.
        /// </summary>
        public TestDiscoverer( TestFactory factory, Action<string>? trace = null )
        {
            this._factory = factory;
            this._trace = trace;
        }

        /// <inheritdoc />
        public ITestAssembly TestAssembly => this._factory.TestAssembly;

        /// <summary>
        /// Discovers the test files of a directory of the test project and of its subdirectories, and returns their test cases.
        /// </summary>
        public List<TestCase> Discover( string? subDirectory, ImmutableHashSet<string> excludedDirectories )
        {
            List<TestCase> testCases = new();
            this.Discover( testCases.Add, subDirectory, false, excludedDirectories );

            return testCases;
        }

        /// <summary>
        /// Adds the test cases of a directory and of its subdirectories to a list.
        /// </summary>
        private void Discover(
            Action<TestCase> onTestCaseDiscovered,
            string? subDirectory,
            bool isXUnitFrameworkDiscovery,
            ImmutableHashSet<string> excludedSubdirectories )
        {
            var sync = new object();

            this._trace?.Invoke( $"Discovering tests in directory '{subDirectory}'." );

            var factory = this._factory;
            var projectProperties = factory.ProjectProperties;
            var reader = factory.DirectoryOptionsReader;
            var fileSystem = factory.FileSystem;

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
                        this._trace?.Invoke( $"Child directory '{directory}' excluded because of the Exclude option." );

                        return;
                    }

                    this._trace?.Invoke( $"Processing directory '{directory}'." );

                    // If the directory is included, index the files.
                    const string runnerFileName = "_Runner.cs";

                    foreach ( var testPath in fileSystem.EnumerateFiles( directory, "*.cs" ) )
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

                        this._trace?.Invoke( $"Including the file '{testPath}'" );

                        var testCase = new TestCase( factory, fileSystem.GetRelativePath( projectProperties.SourceDirectory, testPath ) );

                        this._trace?.Invoke( $"    {testCase.TestClassName} / {testCase.TestMethodName} / {testCase.TestCaseDisplayName}" );

                        lock ( sync )
                        {
                            onTestCaseDiscovered( testCase );
                        }
                    }

                    // Process children directories.

                    foreach ( var nestedDir in fileSystem.EnumerateDirectories( directory ) )
                    {
                        if ( excludedSubdirectories.Contains( nestedDir ) )
                        {
                            this._trace?.Invoke( $"Child directory '{nestedDir}' excluded because it is covered by other tests." );

                            continue;
                        }

                        if ( !isXUnitFrameworkDiscovery )
                        {
                            // Don't include a directory that has a _Runner file.
                            var runnerFile = Path.Combine( nestedDir, runnerFileName );

                            if ( File.Exists( runnerFile ) )
                            {
                                this._trace?.Invoke( $"Child directory '{nestedDir}' excluded it contains '{runnerFileName}'." );

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

        /// <summary>
        /// Discovers all tests of the assembly and reports them to xunit.
        /// </summary>
        /// <remarks>
        /// When xunit asks for the tests of specific CLR types, no test is reported, because the tests of this framework are
        /// files and not members of CLR types.
        /// </remarks>
        public async ValueTask Find(
            Func<ITestCase, ValueTask<bool>> callback,
            ITestFrameworkDiscoveryOptions discoveryOptions,
            Type[]? types = null,
            CancellationToken? cancellationToken = null )
        {
            if ( types is { Length: > 0 } )
            {
                return;
            }

            var testCases = new List<TestCase>();
            this.Discover( testCases.Add, null, true, ImmutableHashSet<string>.Empty );

            foreach ( var testCase in testCases )
            {
                if ( cancellationToken?.IsCancellationRequested == true )
                {
                    return;
                }

                if ( !await callback( testCase ) )
                {
                    return;
                }
            }
        }
    }
}
