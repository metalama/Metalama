internal class TargetClass
{
  private int _property;
  [Override]
  public int Property
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      Console.WriteLine("This is the original getter.");
      return _property;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      Console.WriteLine("This is the original setter.");
      _property = value;
    }
  }
  private static int _staticProperty;
  [Override]
  public static int StaticProperty
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      Console.WriteLine("This is the original getter.");
      return _staticProperty;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      Console.WriteLine("This is the original setter.");
      _staticProperty = value;
    }
  }
}