internal class TargetClass
{
  [Test]
  public int BlockBodiedAccessors
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
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
      global::System.Console.WriteLine("This is the overridden getter.");
      return 42;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      Console.WriteLine("Original");
    }
  }
  [Test]
  public int ExpressionBodiedProperty
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return 42;
    }
  }
  private int _autoProperty;
  [Test]
  public int AutoProperty
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
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
      global::System.Console.WriteLine("This is the overridden getter.");
      return this._autoGetOnlyProperty;
    }
    private init
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      this._autoGetOnlyProperty = value;
    }
  }
}