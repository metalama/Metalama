[Override]
public class TargetClass
{
  public TargetClass(int p)
  {
    global::System.Console.WriteLine("This is the override 2.");
    global::System.Console.WriteLine("This is the contract 2.");
    global::System.Console.WriteLine("This is the override 1.");
    global::System.Console.WriteLine("This is the contract 1.");
    Console.WriteLine($"This is the original constructor.");
  }
}