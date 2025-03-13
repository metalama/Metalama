[ApplyAspect]
internal record MyRecord(int A, int B)
{
  private readonly int _a;
  public int A
  {
    get
    {
      global::System.Console.WriteLine("MyAspect");
      return this._a;
    }
    init
    {
      global::System.Console.WriteLine("MyAspect");
      this._a = value;
    }
  }
  public int B
  {
    get
    {
      global::System.Console.WriteLine("MyAspect");
      Console.WriteLine("Original.");
      return 42;
    }
    init
    {
      global::System.Console.WriteLine("MyAspect");
      Console.WriteLine("Original.");
    }
  }
}