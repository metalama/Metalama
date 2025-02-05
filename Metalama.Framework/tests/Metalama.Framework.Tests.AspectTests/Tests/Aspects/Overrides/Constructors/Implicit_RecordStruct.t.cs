[Override]
public record struct TargetStruct
{
  public TargetStruct()
  {
    global::System.Console.WriteLine("This is the override.");
  }
}