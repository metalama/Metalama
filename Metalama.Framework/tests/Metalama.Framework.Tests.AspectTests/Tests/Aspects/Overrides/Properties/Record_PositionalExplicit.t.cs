[ApplyAspect]
internal record MyRecord(int A, int B)
{
  private readonly int _a;
  public int A
  {
    get
    {
      return this._a;
    }
    init
    {
      this._a = value;
    }
  }
  public int B
  {
    get
    {
      Console.WriteLine("Original.");
      return 42;
    }
    init
    {
      Console.WriteLine("Original.");
    }
  }
}