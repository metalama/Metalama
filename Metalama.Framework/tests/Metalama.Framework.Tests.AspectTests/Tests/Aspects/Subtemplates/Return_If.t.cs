internal class TargetCode
{
  [Aspect]
  private void Method()
  {
    global::System.Console.WriteLine("Shold return? False");
    global::System.Console.WriteLine("Shold return? True");
    return;
    return;
  }
}