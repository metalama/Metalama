[Override]
public class TargetClass
{
  static TargetClass()
  {
    global::System.Console.WriteLine("This is the override.");
    Console.WriteLine("This is the original static constructor.");
  }
}