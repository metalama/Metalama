[DependencyAspect]
[InitializerAspect]
public class ImplicitConstructor
{
  public ImplicitConstructor([AspectGenerated] IFormatProvider? formatProvider = null, [AspectGenerated] InitializationContext context = default)
  {
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  protected virtual void OnConstructed(InitializationContext context = default)
  {
    Console.WriteLine("OnConstructed ImplicitConstructor");
  }
}
[DependencyAspect]
[InitializerAspect]
public class ExplicitConstructors
{
  public ExplicitConstructors([AspectGenerated] IFormatProvider? formatProvider = default, [AspectGenerated] InitializationContext context = default)
  {
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  public ExplicitConstructors(int x, [AspectGenerated] IFormatProvider? formatProvider = default, [AspectGenerated] InitializationContext context = default)
  {
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  protected virtual void OnConstructed(InitializationContext context = default)
  {
    Console.WriteLine("OnConstructed ExplicitConstructors");
  }
}