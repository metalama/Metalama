[TheAspect]
public class TargetCode
{
  public TargetCode(int value, [AspectGenerated] InitializationContext context = default)
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
      if (!context.IsHandled(InitializationSlot.OnConstructed))
      {
        this.OnConstructed(context);
      }
  }
  protected virtual void OnConstructed(InitializationContext context = default)
  {
    Console.WriteLine("OnConstructed!");
  }
}