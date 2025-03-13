internal class C
{
  private readonly int _property1;
  public int Property1
  {
    [MyAspect]
    get
    {
      global::System.Console.WriteLine("Overridden.");
      return this._property1;
    }
    private init
    {
      this._property1 = value;
    }
  }
  public int Property2
  {
    [MyAspect]
    set
    {
      global::System.Console.WriteLine("Overridden.");
      return;
    }
  }
  private int _property3;
  public int Property3
  {
    [MyAspect]
    get
    {
      global::System.Console.WriteLine("Overridden.");
      return this._property3;
    }
    [MyAspect]
    set
    {
      global::System.Console.WriteLine("Overridden.");
      this._property3 = value;
      return;
    }
  }
  public event Action Event1
  {
    [MyAspect]
    add
    {
      global::System.Console.WriteLine("Overridden.");
    }
    [MyAspect]
    remove
    {
      global::System.Console.WriteLine("Overridden.");
      return;
    }
  }
  public event Action Event2
  {
    [MyAspect]
    add
    {
      global::System.Console.WriteLine("Overridden.");
      return;
    }
    remove
    {
    }
  }
  public event Action Event3
  {
    add
    {
    }
    [MyAspect]
    remove
    {
      global::System.Console.WriteLine("Overridden.");
      return;
    }
  }
  public int this[int x]
  {
    [MyAspect]
    get
    {
      global::System.Console.WriteLine("Overridden.");
      Console.WriteLine("Original");
      return x;
    }
  }
  public int this[int x, int y]
  {
    [MyAspect]
    set
    {
      global::System.Console.WriteLine("Overridden.");
      Console.WriteLine("Original");
      return;
    }
  }
  public int this[int x, int y, int z]
  {
    [MyAspect]
    get
    {
      global::System.Console.WriteLine("Overridden.");
      Console.WriteLine("Original");
      return x + y;
    }
    [MyAspect]
    set
    {
      global::System.Console.WriteLine("Overridden.");
      Console.WriteLine("Original");
      return;
    }
  }
}