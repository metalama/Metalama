[AspectA]
[AspectB]
public class BaseClass
{
  public BaseClass([AspectGenerated] InitializationContext context)
  {
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  public BaseClass() : this(context: default)
  {
  }
  protected virtual void OnConstructed(InitializationContext context = default)
  {
    Console.WriteLine("AspectA.BeforeBase");
    Console.WriteLine("AspectB.BeforeBase");
    Console.WriteLine("AspectB.AfterBase");
    Console.WriteLine("AspectA.AfterBase");
  }
}