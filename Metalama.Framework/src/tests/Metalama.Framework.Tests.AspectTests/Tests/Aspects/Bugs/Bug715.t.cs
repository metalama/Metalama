[Aspect]
internal class Target
{
  public int X { get; }
  public Target(int x)
  {
    this.X = x;
    Console.WriteLine("initialized");
  }
}