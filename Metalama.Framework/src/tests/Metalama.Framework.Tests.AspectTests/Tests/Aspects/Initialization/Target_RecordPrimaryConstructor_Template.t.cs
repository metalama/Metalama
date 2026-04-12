[Aspect]
public record TargetRecord
{
  private int Method(int a)
  {
    return a;
  }
  public TargetRecord()
  {
    Console.WriteLine("TargetRecord: Aspect");
  }
}