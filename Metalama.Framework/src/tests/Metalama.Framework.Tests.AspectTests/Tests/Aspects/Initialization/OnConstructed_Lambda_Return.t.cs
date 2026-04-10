[TheAspect]
public class TargetCode
{
  public TargetCode(int value, [AspectGenerated] InitializationContext context = default)
  {
    // `return;` inside a nested lambda belongs to the lambda's own control flow and
    // must NOT be rewritten. The constructor has no top-level return statement, so
    // no epilogue label should be emitted.
    Action action = () =>
    {
      if (value < 0)
      {
        return;
      }
      Console.WriteLine(value);
    };
    action();
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