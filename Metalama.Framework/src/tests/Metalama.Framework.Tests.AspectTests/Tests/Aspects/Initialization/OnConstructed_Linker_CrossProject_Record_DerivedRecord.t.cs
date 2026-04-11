public record DerivedRecord : BaseRecord
{
  public void Deconstruct(out int X, out int Y)
  {
    X = this.X;
    Y = this.Y;
  }
  public int Y { get; init; }
  public DerivedRecord(int X, int Y, [AspectGenerated] InitializationContext context = default) : base(X, context.Descend(InitializationSlot.OnConstructed))
  {
    this.Y = Y;
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
}