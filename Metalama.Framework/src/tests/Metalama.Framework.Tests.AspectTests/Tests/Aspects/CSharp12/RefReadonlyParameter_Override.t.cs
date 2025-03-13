using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp12.RefReadonlyParameter_Override;
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
internal class TheAspect : OverrideMethodAspect
{
  public override dynamic? OverrideMethod() => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
}
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
internal class C
{
  [TheAspect]
  private void M(in int i, ref readonly int j)
  {
    global::System.Console.WriteLine($"C.M(in int, ref readonly int)/i: Kind=In, Value={i}");
    global::System.Console.WriteLine($"C.M(in int, ref readonly int)/j: Kind=RefReadOnly, Value={j}");
    Console.WriteLine(i + j);
    return;
  }
}