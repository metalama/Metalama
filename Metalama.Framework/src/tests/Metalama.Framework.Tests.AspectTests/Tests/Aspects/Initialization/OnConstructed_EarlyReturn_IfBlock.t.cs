[TheAspect]
public class TargetCode
{
  public TargetCode(int value, [global::Metalama.Framework.RunTime.AspectGeneratedAttribute] global::Metalama.Framework.RunTime.Initialization.InitializationContext context = default(global::Metalama.Framework.RunTime.Initialization.InitializationContext))
  {
    if (value < 0)
    {
      goto __epilogue;
    }
    Console.WriteLine(value);
    __epilogue:
      if (!context.IsHandled(global::Metalama.Framework.RunTime.Initialization.InitializationSlot.OnConstructed))
      {
        this.OnConstructed(context);
      }
  }
  public virtual void OnConstructed(global::Metalama.Framework.RunTime.Initialization.InitializationContext context = default)
  {
    global::System.Console.WriteLine("OnConstructed!");
  }
}