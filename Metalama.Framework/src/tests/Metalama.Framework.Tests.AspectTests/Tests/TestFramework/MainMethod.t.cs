using System;
using Metalama.Framework.Aspects;
namespace Metalama.Framework.Tests.AspectTests.Tests.TestFramework.MainMethod;
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
public class LogAttribute : OverrideMethodAspect
{
  public override dynamic? OverrideMethod() => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
}
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
internal class Program
{
  [Log]
  public static void Main()
  {
    global::System.Console.WriteLine("Entering Main");
    try
    {
      Console.WriteLine("Hello");
      return;
    }
    finally
    {
      global::System.Console.WriteLine("Leaving Main");
    }
  }
}
