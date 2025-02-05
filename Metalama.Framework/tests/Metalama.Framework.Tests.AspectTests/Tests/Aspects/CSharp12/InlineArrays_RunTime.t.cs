using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects; 
using Metalama.Framework.Code;
namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp12.InlineArrays_RunTime;
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
public class TheAspect : OverrideMethodAspect
{
  public override void BuildAspect(IAspectBuilder<IMethod> builder) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
  public override dynamic? OverrideMethod() => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
}
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
#pragma warning disable CS0436 // Type conflicts with imported type
[InlineArray(10)]
#pragma warning restore CS0436
public struct Buffer
{
  private int _element0;
}
public class C
{
  [TheAspect]
  void M()
  {
    var buffer_1 = new global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp12.InlineArrays_RunTime.Buffer();
    for (int i_1 = 0; i_1 < 10; i_1++)
    {
      buffer_1[i_1] = i_1;
    }
    foreach (var i_2 in buffer_1)
    {
      global::System.Console.WriteLine(i_2);
    }
    var buffer = new Buffer();
    for (int i = 0; i < 10; i++)
    {
      buffer[i] = i;
    }
    foreach (var i in buffer)
    {
      Console.WriteLine(i);
    }
    return;
  }
}