[MyAspect]
internal class C
{
  public C([My] int x, int introduced = 42)
  {
    Console.WriteLine($"x={x}");
  }
}