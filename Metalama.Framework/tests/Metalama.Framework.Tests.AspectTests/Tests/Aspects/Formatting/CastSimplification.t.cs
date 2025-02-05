using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
#pragma warning disable CS0169 // Field is not used
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Formatting.CastSimplification;
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
internal class Aspect : OverrideMethodAspect
{
  public override dynamic? OverrideMethod() => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
}
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
internal class TargetCode : ICloneable
{
  private string? s;
  private TargetCode? tc;
  [Aspect]
  private TargetCode Method()
  {
    var clone = (TargetCode)MemberwiseClone();
    clone.s = (string? )s.Clone();
    clone.tc = (TargetCode? )((ICloneable)tc).Clone();
    return clone;
  }
  object ICloneable.Clone() => new();
}