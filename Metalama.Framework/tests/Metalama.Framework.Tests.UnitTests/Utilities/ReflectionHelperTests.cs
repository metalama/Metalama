// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Utilities.Roslyn;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Utilities;

public class ReflectionHelperTests
{
    [Fact]
    public void AssemblyNameToAssemblyIdentity()
    {
        var assemblyName = typeof(IAspect).Assembly.GetName();
        var assemblyIdentity = assemblyName.ToAssemblyIdentity();

        Assert.Equal( assemblyName.Name, assemblyIdentity.Name );
        Assert.Equal( assemblyName.Version, assemblyIdentity.Version );
        Assert.Equal( assemblyName.CultureName, assemblyIdentity.CultureName );
        Assert.Equal( assemblyName.GetPublicKeyToken(), assemblyIdentity.PublicKeyToken.ToArray() );
    }
}