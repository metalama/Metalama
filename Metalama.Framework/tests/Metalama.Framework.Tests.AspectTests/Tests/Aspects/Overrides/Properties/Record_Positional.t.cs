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
  private readonly int _b = B;
  public int B
  {
    get
    {
      return this._b;
    }
    init
    {
      this._b = value;
    }
  }
}