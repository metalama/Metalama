[Aspect]
public record TargetRecord
{
  private int Method(int a)
  {
    return a;
  }
  public TargetRecord()
  {
    global::System.Console.WriteLine("TargetRecord: Aspect");
  }
}