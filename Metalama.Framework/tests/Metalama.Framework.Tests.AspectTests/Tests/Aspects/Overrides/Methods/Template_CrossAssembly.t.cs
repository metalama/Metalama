[TestAspect]
internal class TargetClass
{
  public void VoidMethod()
  {
    global::System.Console.WriteLine("Aspect code");
    return;
  }
  public int Method(int x)
  {
    global::System.Console.WriteLine("Aspect code");
    return x;
  }
}