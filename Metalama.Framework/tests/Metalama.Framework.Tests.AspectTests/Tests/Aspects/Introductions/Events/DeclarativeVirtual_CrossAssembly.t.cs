[InheritedIntroduction]
internal class TargetClass
{
  public event global::System.EventHandler VirtualIntroduction
  {
    add
    {
      global::System.Console.WriteLine("Base template (expected).");
    }
    remove
    {
      global::System.Console.WriteLine("Base template (expected).");
    }
  }
  public event global::System.EventHandler VirtualOverriddenIntroduction
  {
    add
    {
      global::System.Console.WriteLine("Base template (expected).");
    }
    remove
    {
      global::System.Console.WriteLine("Base template (expected).");
    }
  }
}