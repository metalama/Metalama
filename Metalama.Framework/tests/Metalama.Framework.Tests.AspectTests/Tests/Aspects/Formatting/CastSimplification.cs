using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable CS0169 // Field is not used
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Formatting.CastSimplification;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        var clone = meta.Cast( meta.Target.Type, meta.Base.MemberwiseClone() );

        foreach (var field in meta.Target.Type.Fields)
        {
            field.With( (IExpression)clone ).Value = meta.Cast( field.Type, ( (ICloneable)field.Value! ).Clone() );
        }

        return clone;
    }
}

internal class TargetCode : ICloneable
{
    private string? s;
    private TargetCode? tc;

    [Aspect]
    private TargetCode Method() => throw new NotImplementedException();

    object ICloneable.Clone() => new();
}