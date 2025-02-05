[ApplyAspect]
internal record MyRecord(int A, int B)
{
  private readonly int _a = A;
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
  private int _b = B;
  public int B
  {
    get
    {
      Console.WriteLine("Original.");
      return _b;
    }
    init
    {
      Console.WriteLine("Original.");
    }
  }
}