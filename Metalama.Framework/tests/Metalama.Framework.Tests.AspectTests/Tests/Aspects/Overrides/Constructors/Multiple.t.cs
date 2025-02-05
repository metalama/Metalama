[InnerOverride]
[OuterOverride]
public class TargetClass
{
  public TargetClass()
  {
    global::System.Console.WriteLine("This is the outer override 2.");
    global::System.Console.WriteLine("This is the outer override 1.");
    global::System.Console.WriteLine("This is the inner override 2.");
    global::System.Console.WriteLine("This is the inner override 1.");
    Console.WriteLine($"This is the original constructor.");
  }
}