internal class TargetClass
{
  [Test]
  public int BlockBodiedAccessors
  {
    get
    {
      return default;
    }
    set
    {
      global::System.Console.WriteLine("Overridden");
    }
  }
  [Test]
  public int ExpressionBodiedAccessors
  {
    get
    {
      return default;
    }
    set
    {
      global::System.Console.WriteLine("Overridden");
    }
  }
  [Test]
  public int ExpressionBodiedProperty
  {
    get
    {
      return default;
    }
  }
  [Test]
  public int AutoProperty
  {
    get
    {
      return default;
    }
    set
    {
      global::System.Console.WriteLine("Overridden");
    }
  }
  [Test]
  public int AutoGetOnlyProperty
  {
    get
    {
      return default;
    }
    private init
    {
      global::System.Console.WriteLine("Overridden");
    }
  }
}