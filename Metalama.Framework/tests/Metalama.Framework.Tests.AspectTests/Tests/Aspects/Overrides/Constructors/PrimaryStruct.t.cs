[Override]
public struct TargetStruct
{
  public TargetStruct(int x, int y)
  {
    global::System.Console.WriteLine("This is the override.");
    global::System.Console.WriteLine($"Param x = {x}");
    global::System.Console.WriteLine($"Param y = {y}");
  }
  public TargetStruct()
  {
    global::System.Console.WriteLine("This is the override.");
  }
}