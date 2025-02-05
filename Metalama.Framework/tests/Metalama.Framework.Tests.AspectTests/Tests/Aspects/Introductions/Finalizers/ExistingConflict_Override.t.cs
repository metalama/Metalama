[Override]
internal class TargetClass
{
  ~TargetClass()
  {
    global::System.Console.WriteLine("This is the introduction.");
    Console.WriteLine("This is the existing finalizer.");
    return;
  }
}