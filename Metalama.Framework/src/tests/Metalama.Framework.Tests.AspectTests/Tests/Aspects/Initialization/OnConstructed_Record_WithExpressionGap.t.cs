[TheAspect]
public sealed record TargetRecord
{
  public void Deconstruct(out int X)
  {
    X = this.X;
  }
  public static int Counter;
  public int X { get; init; }
  public InitializationContext context { get; init; }
  public void Deconstruct(out int X, out InitializationContext context)
  {
    X = this.X;
    context = this.context;
  }
  public TargetRecord(int X, InitializationContext context = default)
  {
    this.X = X;
    this.context = context;
    this.OnConstructed(context);
  }
  private void OnConstructed(InitializationContext context = default)
  {
    Counter++;
  }
}