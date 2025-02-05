[Introduction]
internal class TargetClass(int x)
{
  public TargetClass() : this(42)
  {
    global::System.Console.WriteLine("This is introduced constructor.");
  }
}