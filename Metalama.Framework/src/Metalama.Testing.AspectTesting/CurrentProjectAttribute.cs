// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Testing.AspectTesting.Utilities;
using Metalama.Testing.AspectTesting.XunitFramework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using Xunit.Sdk;

namespace Metalama.Testing.AspectTesting
{
    /// <summary>
    /// An xUnit data attribute that generates test cases from all aspect test files in the current project.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this attribute with <c>[Theory]</c> to automatically discover all test files (<c>*.cs</c>) in the project
    /// and generate a test case for each file. Unlike <see cref="CurrentDirectoryAttribute"/>, this attribute
    /// discovers files across the entire project rather than a specific directory.
    /// </para>
    /// <para>
    /// The attribute excludes:
    /// <list type="bullet">
    /// <item>Subdirectories that contain a file named <c>_Runner.cs</c> (which indicates a separate test class)</item>
    /// <item>Files marked with <c>// @Skipped</c> directive</item>
    /// </list>
    /// </para>
    /// <para>
    /// Test discovery respects options from <c>metalamaTests.json</c> configuration files.
    /// </para>
    /// </remarks>
    /// <seealso cref="CurrentDirectoryAttribute"/>
    /// <seealso cref="AspectTestClass"/>
    /// <seealso cref="TestOptions"/>
    /// <seealso href="@aspect-testing"/>
    [PublicAPI]
    [Obsolete( "This class is obsolete. Use the default aspect test project setup provided by Metalama.Testing.AspectTesting instead." )]
    public sealed class CurrentProjectAttribute : DataAttribute
    {
        public override IEnumerable<object[]> GetData( MethodInfo testMethod )
        {
            var discoverer = new TestDiscoverer( new ReflectionAssemblyInfo( testMethod.DeclaringType!.Assembly ) );

            var projectDirectory = discoverer.GetTestProjectProperties().SourceDirectory;

            foreach ( var testCase in discoverer.Discover( projectDirectory, ImmutableHashSet<string>.Empty ) )
            {
                if ( testCase.SkipReason == null )
                {
                    var relativePath = discoverer.FileSystem.GetRelativePath( projectDirectory, testCase.FullPath );

                    yield return new object[] { relativePath };
                }
            }
        }
    }
}