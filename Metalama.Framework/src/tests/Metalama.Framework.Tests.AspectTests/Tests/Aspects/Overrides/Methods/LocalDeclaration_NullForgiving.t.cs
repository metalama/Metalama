internal class TargetCode
{
  [Aspect]
  private string? Method(string? a)
  {
    global::System.Console.WriteLine("Before");
    global::System.String? result;
    result = a;
    global::System.Console.WriteLine("After");
    return (global::System.String? )result;
  }
}
