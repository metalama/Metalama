// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Testing;
using System;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Infrastructure;

/// <summary>
/// Tests of <see cref="MachineNameMachineIdProvider"/>, the implementation registered on the operating systems for
/// which we know no better identifier.
/// </summary>
public sealed class MachineNameMachineIdProviderTests : TestsBase
{
    public MachineNameMachineIdProviderTests( ITestOutputHelper logger ) : base( logger ) { }

    /// <summary>
    /// Verifies that the identifier is the name of the machine.
    /// </summary>
    [Fact]
    public void MachineIdIsTheMachineName()
    {
        var provider = new MachineNameMachineIdProvider( this.ServiceProvider );

        Assert.Equal( Environment.MachineName, provider.GetUncachedMachineId() );
    }
}
