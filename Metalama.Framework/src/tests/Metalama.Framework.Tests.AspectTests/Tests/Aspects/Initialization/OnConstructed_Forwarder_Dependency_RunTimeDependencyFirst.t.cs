[DependencyAspect]
[InitializerAspect]
public class ImplicitConstructor
{
  public ImplicitConstructor([AspectGenerated] InitializationContext context, [AspectGenerated] IFormatProvider? formatProvider = null)
  {
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  public ImplicitConstructor([AspectGenerated] IFormatProvider? formatProvider = null) : this(context: default, formatProvider)
  {
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
  public ExplicitConstructors([AspectGenerated] InitializationContext context, [AspectGenerated] IFormatProvider? formatProvider = default)
  {
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  public ExplicitConstructors(int x, [AspectGenerated] InitializationContext context = default, [AspectGenerated] IFormatProvider? formatProvider = default)
  {
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  public ExplicitConstructors([AspectGenerated] IFormatProvider? formatProvider = null) : this(context: default, formatProvider)
  {
  }
  protected virtual void OnConstructed(InitializationContext context = default)
  {
    Console.WriteLine("OnConstructed ExplicitConstructors");
  }
}