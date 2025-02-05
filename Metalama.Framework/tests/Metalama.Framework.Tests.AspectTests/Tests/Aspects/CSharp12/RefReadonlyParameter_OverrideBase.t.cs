using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp12.RefReadonlyParameter_OverrideBase;
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
internal class TheAspect : TypeAspect
{
  public override void BuildAspect(IAspectBuilder<INamedType> builder) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
  [Template]
  protected int M(in int i, ref readonly int j) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
}
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
internal class B
{
  protected virtual int M(in int i, ref readonly int j)
  {
    return i + j;
  }
  protected virtual int this[in int i, ref readonly int j] => 42;
}
[TheAspect]
internal class D : B
{
  protected override global::System.Int32 M(in global::System.Int32 i, ref readonly global::System.Int32 j)
  {
    global::System.Console.WriteLine($"D.M(in int, ref readonly int)@i: Kind=In, Value={i}");
    global::System.Console.WriteLine($"D.M(in int, ref readonly int)@j: Kind=RefReadOnly, Value={j}");
    return base.M(i, in j);
  }
  protected override global::System.Int32 this[in global::System.Int32 i, ref readonly global::System.Int32 j]
  {
    get
    {
      global::System.Console.WriteLine($"D.this[in int, ref readonly int].get@i: Kind=In, Value={i}");
      global::System.Console.WriteLine($"D.this[in int, ref readonly int].get@j: Kind=RefReadOnly, Value={j}");
      return base[i, in j];
    }
  }
}