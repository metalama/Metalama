[Override]
internal class TargetClass
{
  ~TargetClass()
  {
    global::System.Console.WriteLine("This is the override.");
    global::System.Console.WriteLine("This is the introduction.");
    return;
  }
}