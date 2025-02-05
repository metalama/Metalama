[Override]
internal class TargetClass
{
  public TargetClass()
  {
    global::System.Console.WriteLine("This is the override 2.");
    global::System.Console.WriteLine("This is the override 1.");
    Console.WriteLine($"This is the original constructor.");
  }
}