public class MiddleClass : BaseClass
{
  public string MiddleProperty { get; set; } = "middle";
  public MiddleClass([AspectGenerated] InitializationContext context = default) : base(context.Descend(InitializationSlot.OnConstructed))
  {
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  protected override void OnConstructed(InitializationContext context = default)
  {
    base.OnConstructed(context);
    Console.WriteLine("OnConstructed MiddleClass:");
    Console.WriteLine($"  MiddleProperty = {MiddleProperty}");
  }
}
public class DerivedClass : MiddleClass
{
  public string DerivedProperty { get; set; } = "derived";
  public DerivedClass([AspectGenerated] InitializationContext context = default) : base(context.Descend(InitializationSlot.OnConstructed))
  {
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  protected override void OnConstructed(InitializationContext context = default)
  {
    base.OnConstructed(context);
    Console.WriteLine("OnConstructed DerivedClass:");
    Console.WriteLine($"  DerivedProperty = {DerivedProperty}");
  }
}