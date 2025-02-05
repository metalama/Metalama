[assembly: MyAspect("Hello, world.")]
public class C
{
  public void M()
  {
    Console.WriteLine("Overridden: Hello, world.");
  }
}