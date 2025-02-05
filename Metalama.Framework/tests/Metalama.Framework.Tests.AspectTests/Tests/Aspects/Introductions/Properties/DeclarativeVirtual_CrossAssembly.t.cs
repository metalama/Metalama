[InheritedIntroduction]
internal class TargetClass
{
  public global::System.Int32 VirtualIntroduction
  {
    get
    {
      global::System.Console.WriteLine("Base template (expected).");
      return (global::System.Int32)42;
    }
    set
    {
      global::System.Console.WriteLine("Base template (expected).");
    }
  }
  public global::System.Int32 VirtualOverriddenIntroduction
  {
    get
    {
      global::System.Console.WriteLine("Base template (expected).");
      return (global::System.Int32)42;
    }
    set
    {
      global::System.Console.WriteLine("Base template (expected).");
    }
  }
}