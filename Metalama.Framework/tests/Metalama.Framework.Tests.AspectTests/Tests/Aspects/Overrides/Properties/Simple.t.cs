internal class TargetClass
{
  private int _field;
  [Override]
  public int Property
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return _field;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      _field = value;
    }
  }
  private static int _staticField;
  [Override]
  public static int StaticProperty
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return _staticField;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      _staticField = value;
    }
  }
}