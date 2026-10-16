public class DerivedClass : BaseClass<int>
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
  protected override void OnConstructed(InitializationContext context = default)
  {
    base.OnConstructed(context);
    Console.WriteLine("OnConstructed DerivedClass");
  }
}