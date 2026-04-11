[TheAspect]
public class TargetCode
{
  public TargetCode(int value, [AspectGenerated] InitializationContext context = default)
  {
    if (value < 0)
    {
      goto epilogue;
    }
    Console.WriteLine(value);
    goto epilogue_1;
    epilogue:
      Console.WriteLine("user epilogue");
    epilogue_1:
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