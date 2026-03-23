[Aspect]
internal class Target
{
  public int X { get; }
  public Target(int x)
  {
    this.X = x;
    global::System.Console.WriteLine("first");
    global::System.Console.WriteLine("second");
    if (x > 0)
    {
      global::System.Console.WriteLine("positive");
    }
  }
}
