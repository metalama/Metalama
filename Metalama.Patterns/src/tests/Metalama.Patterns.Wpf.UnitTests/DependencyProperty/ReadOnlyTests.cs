// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Wpf.UnitTests.Assets.DependencyPropertyNS;
using Xunit;

// ReSharper disable once CheckNamespace
namespace Metalama.Patterns.Wpf.UnitTests.DependencyProperty_;

public sealed class ReadOnlyTests
{
    [Fact]
    public void PublicSetFails()
    {
        var o = new ReadOnlyTestClass();
        Assert.Throws<InvalidOperationException>( () => o.SetValue( ReadOnlyTestClass.NameProperty, "x" ) );
    }

    [Fact]
    public void PrivateSetSucceeds()
    {
        var o = new ReadOnlyTestClass();
        o.SetName( "x" );
    }
}