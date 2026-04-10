[TheAspect]
public class BaseClass
{
  public BaseClass(int x, [global::Metalama.Framework.RunTime.AspectGeneratedAttribute] global::Metalama.Framework.RunTime.Initialization.InitializationContext context = default(global::Metalama.Framework.RunTime.Initialization.InitializationContext))
  {
    _ = x;
    if (!context.IsHandled(global::Metalama.Framework.RunTime.Initialization.InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  public virtual void OnConstructed(global::Metalama.Framework.RunTime.Initialization.InitializationContext context = default)
  {
    global::System.Console.WriteLine("OnConstructed on BaseClass!");
  }
}
public class DerivedClass : BaseClass
{
  public DerivedClass([global::Metalama.Framework.RunTime.AspectGeneratedAttribute] global::Metalama.Framework.RunTime.Initialization.InitializationContext context = default(global::Metalama.Framework.RunTime.Initialization.InitializationContext)) : base(0, context.Descend(global::Metalama.Framework.RunTime.Initialization.InitializationSlot.OnConstructed))
  {
    if (!context.IsHandled(global::Metalama.Framework.RunTime.Initialization.InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  public override void OnConstructed(global::Metalama.Framework.RunTime.Initialization.InitializationContext context = default)
  {
    base.OnConstructed(context);
    global::System.Console.WriteLine("OnConstructed on DerivedClass!");
  }
}