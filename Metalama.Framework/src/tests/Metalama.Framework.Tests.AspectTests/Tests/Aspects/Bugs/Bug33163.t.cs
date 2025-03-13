public class TestClass
{
  [TestAspect]
  public int PrivateSetter
  {
    get
    {
      return (global::System.Int32)42;
    }
    private set
    {
      this.PrivateSetter_Source = value;
      this.PrivateSetter_Source = value;
    }
  }
  private int PrivateSetter_Source
  {
    get
    {
      return 42;
    }
    set
    {
    }
  }
  private int _privateSetter_Auto;
  [TestAspect]
  public int PrivateSetter_Auto
  {
    get
    {
      return (global::System.Int32)42;
    }
    private set
    {
      this._privateSetter_Auto = value;
      this._privateSetter_Auto = value;
    }
  }
  [TestAspect]
  public int PrivateGetter
  {
    private get
    {
      return (global::System.Int32)42;
    }
    set
    {
      this.PrivateGetter_Source = value;
      this.PrivateGetter_Source = value;
    }
  }
  private int PrivateGetter_Source
  {
    get
    {
      return 42;
    }
    set
    {
    }
  }
  private int _privateGetter_Auto;
  [TestAspect]
  public int PrivateGetter_Auto
  {
    private get
    {
      return (global::System.Int32)42;
    }
    set
    {
      this._privateGetter_Auto = value;
      this._privateGetter_Auto = value;
    }
  }
}