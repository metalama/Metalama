public class DerivedClass : BaseClass<int>
{
  public DerivedClass(string name, [global::Metalama.Framework.RunTime.AspectGeneratedAttribute] global::Metalama.Framework.RunTime.Initialization.InitializationContext context = default(global::Metalama.Framework.RunTime.Initialization.InitializationContext)) : base(context.Descend(global::Metalama.Framework.RunTime.Initialization.InitializationSlot.OnConstructed))
  {
    Console.WriteLine(name);
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