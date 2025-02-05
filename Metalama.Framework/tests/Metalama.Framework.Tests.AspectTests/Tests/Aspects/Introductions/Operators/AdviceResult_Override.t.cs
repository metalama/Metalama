[TestAspect]
public class TargetClass
{
  public static global::System.Int32 operator +(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Operators.AdviceResult_Override.TargetClass x, global::System.Int32 y)
  {
    global::System.Console.WriteLine("Aspect code.");
    return default(global::System.Int32);
  }
}