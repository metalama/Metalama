[TheAspect]
public sealed record TargetRecord
{
  public void Deconstruct(out int X)
  {
    X = this.X;
  }
  // Explicit ctor chaining to the primary — only the primary gets the epilogue; this ctor
  // delegates via `: this(0)` so it must be skipped by the `InitializerKind != ConstructorInitializerKind.This` filter.
  public TargetRecord(InitializationContext context = default) : this(0, context)
  {
  }
  public int X { get; init; }
  public TargetRecord(int X, InitializationContext context = default)
  {
    this.X = X;
    this.OnConstructed(context);
  }
  private void OnConstructed(InitializationContext context = default)
  {
    Console.WriteLine("OnConstructed!");
  }
}