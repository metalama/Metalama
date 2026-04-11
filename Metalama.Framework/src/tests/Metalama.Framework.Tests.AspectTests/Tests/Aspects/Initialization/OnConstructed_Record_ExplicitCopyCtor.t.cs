[TheAspect]
public sealed record TargetRecord
{
  public void Deconstruct(out int X)
  {
    X = this.X;
  }
  // User-authored copy constructor. Because it is not `IsImplicitlyDeclared`,
  // `IsRecordCopyConstructor()` returns false, so the epilogue emitter treats it
  // like a normal ctor and emits the OnConstructed call.
  public TargetRecord(TargetRecord original, InitializationContext context = default)
  {
    X = original.X;
    this.OnConstructed(context);
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