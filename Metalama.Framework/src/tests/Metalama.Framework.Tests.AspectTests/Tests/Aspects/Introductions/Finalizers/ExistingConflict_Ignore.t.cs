[Override]
internal class TargetClass
{
  ~TargetClass()
  {
    Console.WriteLine("This is the existing finalizer.");
  }
}