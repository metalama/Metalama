// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Testing.AspectTesting.XunitFramework;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Metalama.Testing.AspectTesting
{
    /// <summary>
    /// The base class for the test suite that is automatically included in user projects.
    /// </summary>
    [PublicAPI]
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