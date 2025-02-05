internal class TargetCode
{
  [Aspect]
  private void VoidMethod()
  {
    global::System.Console.WriteLine("regular template");
    global::System.Console.WriteLine("called template");
    return;
    throw new global::System.Exception();
  }
  [Aspect]
  private int IntMethod()
  {
    global::System.Console.WriteLine("regular template");
    global::System.Console.WriteLine("called template");
    return 42;
    throw new global::System.Exception();
  }
}