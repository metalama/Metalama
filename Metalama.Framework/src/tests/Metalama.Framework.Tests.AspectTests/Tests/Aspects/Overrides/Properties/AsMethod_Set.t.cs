internal class TargetClass
{
  [Override]
  public int Property
  {
    get
    {
      Console.WriteLine("Original getter");
      return 42;
    }
    set
    {
      global::System.Console.WriteLine("Overridden setter");
      Console.WriteLine("Original setter");
    }
  }
  private int _autoProperty;
  [Override]
  public int AutoProperty
  {
    get
    {
      return this._autoProperty;
    }
    set
    {
      global::System.Console.WriteLine("Overridden setter");
      this._autoProperty = value;
    }
  }
}