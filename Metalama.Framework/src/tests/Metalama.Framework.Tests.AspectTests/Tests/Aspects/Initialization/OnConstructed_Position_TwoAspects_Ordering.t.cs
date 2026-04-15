[AspectA]
[AspectB]
public class BaseClass
{
  public BaseClass([AspectGenerated] InitializationContext context = default)
  {
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  protected virtual void OnConstructed(InitializationContext context = default)
  {
    Console.WriteLine("AspectA.BeforeBase");
    Console.WriteLine("AspectB.BeforeBase");
    Console.WriteLine("AspectB.AfterBase");
    Console.WriteLine("AspectA.AfterBase");
  }
}
