[Aspect]
public sealed record TargetRecord
{
  // This ctor chains to the primary via `:this(...)` — the advice must skip it.
  public TargetRecord() : this(0)
  {
  }
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