internal class TargetClass
{
  private int _value;
  // Pragma warning after getter keyword, before body
  [Override]
  public int PropertyWithPragmaAfterGetter
  {
    get
    {
#pragma warning disable CA1822
      global::System.Console.WriteLine("Override getter");
      return _value;
    }
#pragma warning restore CA1822
    set
    {
      global::System.Console.WriteLine("Override setter");
      _value = value;
    }
  }
  // Pragma warning after setter keyword, before body
  [Override]
  public int PropertyWithPragmaAfterSetter
  {
    get
    {
      global::System.Console.WriteLine("Override getter");
      return _value;
    }
    set
    {
#pragma warning disable CA1822
      global::System.Console.WriteLine("Override setter");
      _value = value;
    }
#pragma warning restore CA1822
  }
}
