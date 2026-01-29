internal class C
{
  [TheAspect]
  public void M(Target? t)
  {
    var target = (global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.NullConditionalAssignment_RunTime.Target? )t;
    target?.Property = 42;
    global::System.Console.WriteLine("Intercepted");
    return;
  }
}
