[assembly: MyAspect("1")]
[MyAspect("2")]
public class C
{
  public void M()
  {
    Console.WriteLine("Overridden: 2");
  }
}