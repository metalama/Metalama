[Override]
internal class TargetClass
{
  ~TargetClass()
  {
    global::System.Console.WriteLine("This is the override.");
    Console.WriteLine($"This is the original finalizer.");
    return;
  }
}