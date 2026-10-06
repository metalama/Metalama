// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.AddWithFlagsEnumArgs;

// A value of a flags enumeration that no single member has is written as the bitwise combination of members, in ascending order of value. A
// member that combines several bits is preferred to its bits. A value that the members do not compose, and a value of an enumeration that is
// not a flags enumeration, are written as a cast.

[RunTimeOrCompileTime]
[Flags]
public enum Access
{
    None = 0,
    Read = 1,
    Write = 2,
    ReadWrite = 3,
    Delete = 4
}

[RunTimeOrCompileTime]
public enum Plain
{
    A = 1,
    B = 2
}

public class MyAttribute : Attribute
{
    public MyAttribute( Access composed, Access withCombinedMember, Access notComposed, Plain notFlags ) { }
}

public class MyAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
        => builder.IntroduceAttribute(
            AttributeConstruction.Create(
                typeof(MyAttribute),
                constructorArguments: new object?[] { Access.Read | Access.Delete, (Access) 7, (Access) 8, (Plain) 3 } ) );
}

// <target>
internal class C
{
    [MyAspect]
    private void M() { }
}
