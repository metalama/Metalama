#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_4_8_0_OR_GREATER)
// @RequiredConstant(NET7_0_OR_GREATER)
#endif

#if ROSLYN_4_8_0_OR_GREATER && NET7_0_OR_GREATER
#pragma warning disable CS0169

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects; 
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.Target_ClassWithPrimaryConstructor_Statement;

public class Aspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        for (int i = 1; i <= 19; i++)
        {
            builder.AddInitializer( StatementFactory.Parse($"x{i} = {i};"), InitializerKind.BeforeInstanceConstructor);
        }
    }
}

class MyAttribute : Attribute { }

class Base
{
    public int x2;

    public virtual int x13 { get; }
    public virtual int x14 { get; }
    public int x15 { get; }
}

#pragma warning disable CS0414

// <target>
[Aspect]
abstract class TargetCode() : Base
{
    int x1;
    public new int x2;
    private readonly int x3;
    public required int x4;
    [MyAttribute]
    private protected int x5;

    int x6 { get; }
    int x7 { get; set; }
    public int x8 { get; private set; }
    public int x9 { get; protected set; }
    protected internal int x10 { get; init; }
    public required int x11 { get; init; }
    public virtual int x12 { get; }
    public override int x13 { get; }
    public sealed override int x14 { get; }
    new int x15 { get; }
    [MyAttribute]
    int x16 { get; }
    [field: MyAttribute]
    int x17 { get; }

    int x18, x19;
}

#endif