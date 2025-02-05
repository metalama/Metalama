using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code.SyntaxBuilders;
namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.Issue31089;
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
public class MyAspect : TypeAspect
{
  [Introduce]
  public void Method() => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
}
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
[MyAspect]
internal class C
{
  public void Method()
  {
    global::System.Console.WriteLine($"MachineName={(global::System.Environment.MachineName)}");
  }
}