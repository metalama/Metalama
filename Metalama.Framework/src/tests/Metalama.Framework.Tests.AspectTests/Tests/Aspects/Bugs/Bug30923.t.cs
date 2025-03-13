[TestAspect]
public readonly struct TestStruct
{
  public TestStruct()
  {
    this._test = default;
    Test = 0;
  }
  public TestStruct(int test)
  {
    this._test = default;
    Test = test;
  }
  private readonly int _test;
  public int Test
  {
    get
    {
      return (global::System.Int32)42;
    }
    private init
    {
      this._test = value;
    }
  }
}