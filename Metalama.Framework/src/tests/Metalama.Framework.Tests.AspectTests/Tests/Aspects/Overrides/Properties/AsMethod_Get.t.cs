internal class TargetClass
{
  [Override]
  public int Property
  {
    get
    {
      global::System.Console.WriteLine("Overridden getter");
      Console.WriteLine("Original getter");
      return 42;
    }
    set
    {
      Console.WriteLine("Original setter");
    }
  }
  private int _autoProperty;
  [Override]
  public int AutoProperty
  {
    get
    {
      global::System.Console.WriteLine("Overridden getter");
      return this._autoProperty;
    }
    set
    {
      this._autoProperty = value;
    }
  }
  [Override]
  public int ExpressionProperty
  {
    get
    {
      global::System.Console.WriteLine("Overridden getter");
      return 24;
    }
  }
}