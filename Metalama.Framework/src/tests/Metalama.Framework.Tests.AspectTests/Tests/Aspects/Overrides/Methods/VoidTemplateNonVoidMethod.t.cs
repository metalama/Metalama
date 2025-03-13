internal class TargetClass
{
  [Override]
  private void VoidMethod(object arg)
  {
    if (arg == null)
    {
      global::System.Console.WriteLine("error");
      return;
    }
    Console.WriteLine("void method");
    return;
  }
  [Override]
  private int IntMethod(object arg)
  {
    if (arg == null)
    {
      global::System.Console.WriteLine("error");
      return default;
    }
    Console.WriteLine("int method");
    return 42;
  }
}