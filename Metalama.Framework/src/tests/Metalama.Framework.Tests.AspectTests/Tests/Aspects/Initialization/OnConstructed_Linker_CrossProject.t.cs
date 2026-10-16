public class DerivedClass : BaseClass
{
  public DerivedClass([AspectGenerated] InitializationContext context) : base(context.Descend(InitializationSlot.OnConstructed))
  {
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  public DerivedClass() : this(context: default)
  {
  }
}