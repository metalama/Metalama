[Introduction]
internal class TargetClass
{
  public global::System.Int32 Property
  {
    get
    {
      global::System.Console.WriteLine("Introduced property getter.");
      return (global::System.Int32)42;
    }
    set
    {
      global::System.Console.WriteLine("Introduced property setter.");
    }
  }
}
