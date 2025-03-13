[FirstOverride]
[SecondOverride]
internal class TargetClass
{
  ~TargetClass()
  {
    global::System.Console.WriteLine("This is the first override.");
    global::System.Console.WriteLine("This is the second override.");
    Console.WriteLine($"This is the original finalizer.");
    return;
  }
}