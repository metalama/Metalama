using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
#pragma warning disable CS0618 // Type or member is obsolete
namespace Metalama.Framework.Tests.AspectTests.Aspects.Misc.IndexAndRange
{
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
  public class UseIndexAndRangeAttribute : OverrideMethodAspect
  {
    public override dynamic? OverrideMethod() => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
    [CompileTime]
    private void GetDataClassProperties(INamedType baseType) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
  }
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
  internal class GenericType<T1, T2>
  {
  }
  internal class TargetCode : GenericType<int, int>
  {
    [UseIndexAndRange]
    private int Method(int a, int b, int c, int d)
    {
      var runTimeCollection = global::System.MemoryExtensions.AsSpan(new global::System.String[] { "int", "int" });
      global::System.Console.WriteLine("int");
      global::System.Console.WriteLine(1);
      var runTimeCollectionWithRunTimeIndex = runTimeCollection[^1];
      global::System.Console.WriteLine(runTimeCollectionWithRunTimeIndex);
      var runTimeCollectionWithRunTimeRange = runTimeCollection[..^1].Length;
      global::System.Console.WriteLine(runTimeCollectionWithRunTimeRange);
      return a;
    }
  }
}