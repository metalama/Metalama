internal class TargetClass
{
  [Test]
  public int BlockBodiedAccessors
  {
    get
    {
      Console.WriteLine("Original");
      return 42;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      Console.WriteLine("Original");
    }
  }
  [Test]
  public int ExpressionBodiedAccessors
  {
    get
    {
      return 42;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      Console.WriteLine("Original");
    }
  }
  [Test]
  public int ExpressionBodiedProperty => 42;
  private int _autoProperty;
  [Test]
  public int AutoProperty
  {
    get
    {
      return this._autoProperty;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      this._autoProperty = value;
    }
  }
  private readonly int _autoGetOnlyProperty;
  [Test]
  public int AutoGetOnlyProperty
  {
    get
    {
      return this._autoGetOnlyProperty;
    }
    private init
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      this._autoGetOnlyProperty = value;
    }
  }
}