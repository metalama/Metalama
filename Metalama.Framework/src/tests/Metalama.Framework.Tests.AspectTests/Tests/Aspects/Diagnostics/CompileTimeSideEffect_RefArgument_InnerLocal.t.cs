internal class Target
{
  [Aspect]
  private void M(string? s)
  {
    var x = s;
    if (x != null)
    {
      global::System.Console.WriteLine(42);
    }
    return;
  }
}
