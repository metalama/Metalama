// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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