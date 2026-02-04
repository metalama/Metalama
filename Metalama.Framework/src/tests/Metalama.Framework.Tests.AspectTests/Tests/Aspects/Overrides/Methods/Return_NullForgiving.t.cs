internal class TargetCode
{
  [Aspect]
  private string? Method(string? a)
  {
    global::System.Console.WriteLine("Before");
    return a;
  }
}
