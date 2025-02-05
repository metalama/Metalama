[Aspect]
public struct TargetStruct
{
  private int Method(int a)
  {
    return a;
  }
  public TargetStruct()
  {
    global::System.Console.WriteLine("TargetStruct: Aspect");
  }
}