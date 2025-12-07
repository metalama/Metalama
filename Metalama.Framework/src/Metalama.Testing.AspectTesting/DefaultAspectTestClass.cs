// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Testing.AspectTesting.XunitFramework;
using System;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Metalama.Testing.AspectTesting
{
    /// <summary>
    /// A base class for snapshot test suites that automatically discover test files in the project's source directory.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class is intended for use when an xUnit test assembly is set up via the <c>Metalama.Testing.AspectTesting</c>
    /// package. It automatically discovers test files using the project directory configured in the assembly metadata.
    /// </para>
    /// <para>
    /// For manual test file discovery or when using <see cref="CurrentDirectoryAttribute"/> to specify subdirectories,
    /// derive from <see cref="AspectTestClass"/> instead.
    /// </para>
    /// </remarks>
    /// <seealso cref="AspectTestClass"/>
    /// <seealso cref="CurrentProjectAttribute"/>
    /// <seealso href="@aspect-testing"/>
    [PublicAPI]
    [Obsolete( "This class is obsolete. Use the default snapshot test project setup provided by Metalama.Testing.AspectTesting instead." )]
    public abstract class DefaultAspectTestClass : AspectTestClass
    {
        protected DefaultAspectTestClass( ITestOutputHelper logger ) : base( logger ) { }

        protected override string GetDirectory( string callerMemberName )
        {
            var assemblyInfo = new ReflectionAssemblyInfo( this.GetType().Assembly );
            var discoverer = new TestDiscoverer( assemblyInfo );

            return discoverer.GetTestProjectProperties().SourceDirectory;
        }
    }
}