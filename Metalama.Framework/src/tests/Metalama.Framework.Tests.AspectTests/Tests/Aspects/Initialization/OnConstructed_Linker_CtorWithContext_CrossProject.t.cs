public class DerivedClass : BaseClass
{
  public DerivedClass(int value, InitializationContext context = default) : base(value, context.Descend(global::Metalama.Framework.RunTime.Initialization.InitializationSlot.OnConstructed))
  {
    if (!context.IsHandled(global::Metalama.Framework.RunTime.Initialization.InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  public override void OnConstructed(global::Metalama.Framework.RunTime.Initialization.InitializationContext context = default)
  {
    base.OnConstructed(context);
    global::System.Console.WriteLine("OnConstructed DerivedClass");
  }
}