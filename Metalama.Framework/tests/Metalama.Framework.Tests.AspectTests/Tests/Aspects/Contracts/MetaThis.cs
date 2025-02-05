using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.MetaThis;

public class ActionSpeed : ContractAspect
{
    public override void Validate( dynamic? value )
    {
        if (meta.This is IBuffable)
        {
            //apply action speed modifiers
            value = value * 2;
        }
    }
}

internal interface IBuffable { }

// <target>
internal class Target
{
    private int MaybeBuff( [ActionSpeed] int speed ) => speed;
}