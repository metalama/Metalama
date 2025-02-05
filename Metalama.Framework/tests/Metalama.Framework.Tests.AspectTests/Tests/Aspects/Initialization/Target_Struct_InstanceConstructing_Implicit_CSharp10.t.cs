[Aspect]
public struct TargetStruct
{
  private int Method(int a)
  {
    return a;
  }
  public TargetStruct()
  {
    this = default;
    global::System.Console.WriteLine("TargetStruct: Aspect");
  }
}