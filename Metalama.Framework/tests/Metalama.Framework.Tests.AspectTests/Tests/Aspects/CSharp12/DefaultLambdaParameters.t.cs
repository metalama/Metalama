using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp12.DefaultLambdaParameters;
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
public class TheAspect : OverrideMethodAspect
{
  public override void BuildAspect(IAspectBuilder<IMethod> builder) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
  public override dynamic? OverrideMethod() => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
}
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
public class C
{
  [TheAspect]
  private void M()
  {
    var addWithDefault_1 = (int addTo_1 = 2) => addTo_1 + 1;
    addWithDefault_1();
    addWithDefault_1(5);
    var counter_1 = (params int[] xs_1) => xs_1.Length;
    counter_1();
    counter_1(1, 2, 3);
    var addWithDefault2_1 = AddWithDefaultMethod_1;
    addWithDefault2_1();
    addWithDefault2_1(5);
    var counter2_1 = CountMethod_1;
    counter2_1();
    counter2_1(1, 2);
    int AddWithDefaultMethod_1(int addTo_2 = 2)
    {
      return (global::System.Int32)(addTo_2 + 1);
    }
    int CountMethod_1(params int[] xs_2)
    {
      return (global::System.Int32)xs_2.Length;
    }
    var addWithDefault = (int addTo = 2) => addTo + 1;
    addWithDefault();
    addWithDefault(5);
    var counter = (params int[] xs) => xs.Length;
    counter();
    counter(1, 2, 3);
    var addWithDefault2 = AddWithDefaultMethod;
    addWithDefault2();
    addWithDefault2(5);
    var counter2 = CountMethod;
    counter2();
    counter2(1, 2);
    int AddWithDefaultMethod(int addTo = 2)
    {
      return addTo + 1;
    }
    int CountMethod(params int[] xs)
    {
      return xs.Length;
    }
    return;
  }
}