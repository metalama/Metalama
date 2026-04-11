[TheAspect]
public record BaseRecord
{
  public void Deconstruct(out int X)
  {
    X = this.X;
  }
  public int X { get; init; }
  public BaseRecord(int X, [AspectGenerated] InitializationContext context = default)
  {
    this.X = X;
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  protected virtual void OnConstructed(InitializationContext context = default)
  {
    Console.WriteLine("OnConstructed on BaseRecord!");
  }
}
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
  protected override void OnConstructed(InitializationContext context = default)
  {
    base.OnConstructed(context);
    Console.WriteLine("OnConstructed on DerivedRecord!");
  }
}