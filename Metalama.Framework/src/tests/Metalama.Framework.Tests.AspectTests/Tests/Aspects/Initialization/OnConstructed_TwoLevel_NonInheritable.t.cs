[TheAspect]
public class BaseClass
{
  public string Value { get; }
  public BaseClass(string value, [AspectGenerated] InitializationContext context = default)
  {
    this.Value = value;
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  protected virtual void OnConstructed(InitializationContext context = default)
  {
    Console.WriteLine("OnConstructed on BaseClass!");
  }
}
public class DerivedClass : BaseClass
{
  public int Extra { get; }
  public DerivedClass(string value, int extra, [AspectGenerated] InitializationContext context = default) : base(value, context.Descend(InitializationSlot.OnConstructed))
  {
    this.Extra = extra;
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
}
