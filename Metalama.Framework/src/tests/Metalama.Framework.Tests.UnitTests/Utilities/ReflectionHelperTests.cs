// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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