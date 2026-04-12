public class DerivedClass : BaseClass
{
  public DerivedClass(InitializationContext ctx = default) : base(ctx.Descend(InitializationSlot.OnConstructed))
  {
    if (!ctx.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(ctx);
    }
  }
}