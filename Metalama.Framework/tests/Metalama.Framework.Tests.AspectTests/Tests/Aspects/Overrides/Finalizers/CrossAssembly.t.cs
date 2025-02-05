[Override]
[Introduction]
internal class TargetClass
{
  ~TargetClass()
  {
    global::System.Console.WriteLine("Override");
    global::System.Console.WriteLine("Introduced.");
    return;
  }
}