[Aspect]
public sealed record TargetRecord
{
  public int X { get; init; }
  public void Deconstruct(out int X)
  {
    X = this.X;
  }
  public TargetRecord(int X)
  {
    this.X = X;
    Console.WriteLine("TargetRecord: Aspect");
  }
}