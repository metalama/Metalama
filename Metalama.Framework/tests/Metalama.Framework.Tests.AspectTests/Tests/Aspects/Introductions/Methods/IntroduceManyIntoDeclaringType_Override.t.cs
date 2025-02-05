internal class TargetCode
{
  [Aspect]
  private void M()
  {
  }
  [Aspect]
  private void M2()
  {
  }
  private void NewMethod()
  {
    global::System.Console.WriteLine("TargetCode.M2() says hello.");
    global::System.Console.WriteLine("TargetCode.M() says hello.");
  }
}