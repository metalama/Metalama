public record DerivedRecord : BaseRecord
{
  public void Deconstruct(out int X, out int Y)
  {
    X = this.X;
    Y = this.Y;
  }
  public int Y { get; init; }
  public void Deconstruct(out int X, out int Y, out InitializationContext context)
  {
    X = this.X;
    Y = this.Y;
    context = this.context;
  }
  public DerivedRecord(int X, int Y, [AspectGenerated] InitializationContext context = default) : base(X, context.Descend(InitializationSlot.OnConstructed))
  {
    this.Y = Y;
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
}