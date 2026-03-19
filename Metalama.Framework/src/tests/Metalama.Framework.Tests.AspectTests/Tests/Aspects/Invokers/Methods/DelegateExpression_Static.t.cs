public class TargetClass
{
  public static void StaticMethod()
  {
  }
  [DelegateAspect]
  public void Invoker()
  {
    var action = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.DelegateExpression_Static.TargetClass.StaticMethod;
    return;
  }
}
