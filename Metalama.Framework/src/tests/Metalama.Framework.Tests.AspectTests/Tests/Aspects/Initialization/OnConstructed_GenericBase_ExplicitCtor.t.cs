public class DerivedClass : BaseClass<int>
{
  public DerivedClass(string name, [AspectGenerated] InitializationContext context = default) : base(context.Descend(InitializationSlot.OnConstructed))
  {
    Console.WriteLine(name);
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  protected override void OnConstructed(InitializationContext context = default)
  {
    base.OnConstructed(context);
    Console.WriteLine("OnConstructed DerivedClass");
  }
}