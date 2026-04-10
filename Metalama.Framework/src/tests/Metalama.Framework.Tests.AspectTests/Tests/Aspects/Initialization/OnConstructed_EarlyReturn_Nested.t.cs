[TheAspect]
public class TargetCode
{
  public TargetCode(int value, [global::Metalama.Framework.RunTime.AspectGeneratedAttribute] global::Metalama.Framework.RunTime.Initialization.InitializationContext context = default(global::Metalama.Framework.RunTime.Initialization.InitializationContext))
  {
    // Returns nested inside control-flow blocks are still "top-level" with respect
    // to the constructor body and must be redirected to the epilogue.
    if (value < 0)
    {
      try
      {
        if (value == -1)
        {
          goto __epilogue;
        }
      }
      finally
      {
        Console.WriteLine("finally");
      }
    }
    while (value > 100)
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