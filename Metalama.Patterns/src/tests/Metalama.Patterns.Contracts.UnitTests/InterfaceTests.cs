// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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