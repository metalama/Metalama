// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Add_Type;

public class MyAttribute : Attribute { }

[Inheritable]
public class MyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceAttribute( AttributeConstruction.Create( typeof(MyAttribute) ) );
    }
}

// <target>
internal class TargetClass
{
    [MyAspect]
    internal class C { }

    internal class D : C { }
}

// <target>
internal struct TargetStruct
{
    [MyAspect]
    internal struct C { }
}

// <target>
internal record TargetRecord
{
    [MyAspect]
    internal record C { }

    internal record D : C { }
}

// <target>
internal record class TargetRecordClass
{
    [MyAspect]
    internal record class C { }

    internal record class D : C { }
}

// <target>
internal interface TargetInterface
{
    [MyAspect]
    internal interface C { }
}

// <target>
internal class TargetEnum
{
    [MyAspect]
    internal enum E { }
}

// <target>
internal class TargetDelegate
{
    [MyAspect]
    internal delegate void D();
}