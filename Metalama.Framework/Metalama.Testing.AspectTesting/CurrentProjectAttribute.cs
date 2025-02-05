// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Testing.AspectTesting.Utilities;
using Metalama.Testing.AspectTesting.XunitFramework;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using Xunit.Sdk;

namespace Metalama.Testing.AspectTesting
{
    /// <summary>
    /// An implementation of <c>SyntaxAttribute</c> that generates test cases from files in the current project. To be used with <c>[Theory]</c>.
    /// This attribute will not include subdirectories that contain a file named <c>_Runner.cs</c>.
    /// It also takes into account the <c>metalamaTests.config</c> file.
    /// </summary>
    [PublicAPI]
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