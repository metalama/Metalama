// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Xunit;

namespace Metalama.Patterns.Contracts.UnitTests;

public sealed class InterfaceTests
{
    [Fact]
    public void TestInterfaceImpl()
    {
        var cut = new Foo();
        Assert.Throws<ArgumentNullException>( () => cut.Bar( null! ) );
    }

    // Resharper disable UnusedMemberInSuper.Global
    // Resharper disable UnusedParameter.Global

    private interface IFoo
    {
        void Bar( [Required] string a );
    }

    private sealed class Foo : IFoo
    {
        public void Bar( string a ) { }
    }
}