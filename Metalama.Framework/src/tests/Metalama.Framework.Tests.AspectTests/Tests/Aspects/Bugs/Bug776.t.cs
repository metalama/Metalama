internal class TargetCode
{
  [Log]
  private static void Method()
  {
    global::System.Console.WriteLine("Before Method");
    object result = null;
    global::System.Console.WriteLine("After Method");
    return;
  }
}