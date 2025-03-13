[InheritedIntroduction]
internal class TargetClass
{
  public void VirtualIntroduction()
  {
    global::System.Console.WriteLine("Base template (expected).");
  }
  public void VirtualOverriddenIntroduction()
  {
    global::System.Console.WriteLine("Override template (expected).");
  }
}