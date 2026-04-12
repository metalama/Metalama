[Aspect]
public sealed record TargetRecord
{
  public static int Counter;
  public int X { get; init; }
  public void Deconstruct(out int X)
  {
    X = this.X;
  }
  public TargetRecord(int X)
  {
    this.X = X;
    Counter++;
  }
}