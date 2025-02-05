internal class TargetClass
{
  [TestLiveTemplate(typeof(TestAspect))]
  public void TargetMethod()
  {
    Console.WriteLine("This is the overriding method.");
    Console.WriteLine("This is the original method.");
    return;
  }
}