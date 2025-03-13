internal record MyRecord(int A, int B)
{
  private readonly int _b = B;
  [MyAspect]
  public int B
  {
    get
    {
      global::System.Console.WriteLine("Sob");
      return this._b;
    }
    init
    {
      this._b = value;
    }
  }
}