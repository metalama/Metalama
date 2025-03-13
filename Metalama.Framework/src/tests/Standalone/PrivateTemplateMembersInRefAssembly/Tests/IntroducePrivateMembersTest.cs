// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Dependency;
using System.Reflection;
using Xunit;

namespace Tests;

public partial class IntroducePrivateMembersTest
{
    [Fact]
    public void HasAllExpectedMembers()
    {
        var members = this.GetType().GetMembers(BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.Contains( members, m => m is FieldInfo { Name: "_field" } );
        Assert.Contains( members, m => m is MethodInfo { Name: "Method" } );
        Assert.Contains( members, m => m is PropertyInfo { Name: "AutoProperty" } );
        Assert.Contains( members, m => m is PropertyInfo { Name: "Property" } );
        Assert.Contains( members, m => m is EventInfo { Name: "FieldLikeEvent" } );
        Assert.Contains( members, m => m is EventInfo { Name: "Event" } );
    }

    [IntroducePrivateMembers]
    public void Foo() { }
}
