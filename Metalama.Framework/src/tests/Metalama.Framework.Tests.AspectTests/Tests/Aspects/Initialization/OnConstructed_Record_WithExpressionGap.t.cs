[TheAspect]
public sealed record TargetRecord
{
  public void Deconstruct(out int X)
  {
    X = this.X;
  }
  public static int Counter;
  public int X { get; init; }
  public TargetRecord(int X, InitializationContext context = default)
  {
    this.X = X;
    this.OnConstructed(context);
  }
  private void OnConstructed(InitializationContext context = default)
  {
    Counter++;
  }
}