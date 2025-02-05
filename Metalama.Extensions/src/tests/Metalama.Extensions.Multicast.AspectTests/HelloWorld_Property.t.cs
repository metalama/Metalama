[assembly: MyAspect("Hello, world.")]
public class C
{
  private readonly string _f1 = default !;
  private string _f
  {
    get
    {
      Console.WriteLine("Overridden get: Hello, world.");
      return _f1;
    }
    init
    {
      Console.WriteLine("Overridden set: Hello, world.");
      _f1 = value;
    }
  }
  private int _p;
  public int P
  {
    get
    {
      Console.WriteLine("Overridden get: Hello, world.");
      return _p;
    }
    set
    {
      Console.WriteLine("Overridden set: Hello, world.");
      _p = value;
    }
  }
}