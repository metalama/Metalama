[Aspect]
public struct TargetStruct
{
  private int Method(int a)
  {
    return a;
  }
  public TargetStruct()
  {
    Console.WriteLine("TargetStruct: Aspect");
  }
}