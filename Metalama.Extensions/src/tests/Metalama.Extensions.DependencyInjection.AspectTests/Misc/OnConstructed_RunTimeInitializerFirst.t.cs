[DependencyAspect]
[InitializerAspect]
public class ImplicitConstructor
{
  private IFormatProvider _formatProvider;
  public ImplicitConstructor([AspectGenerated] IFormatProvider? formatProvider = null, [AspectGenerated] InitializationContext context = default)
  {
    this._formatProvider = formatProvider ?? throw new System.ArgumentNullException(nameof(formatProvider));
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  protected virtual void OnConstructed(InitializationContext context = default)
  {
    Console.WriteLine("OnConstructed");
  }
}
[DependencyAspect]
[InitializerAspect]
public class ExplicitConstructors
{
  public ExplicitConstructors([AspectGenerated] IFormatProvider? formatProvider = default, [AspectGenerated] InitializationContext context = default)
  {
    this._formatProvider = formatProvider ?? throw new System.ArgumentNullException(nameof(formatProvider));
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  public ExplicitConstructors(int x, [AspectGenerated] IFormatProvider? formatProvider = default, [AspectGenerated] InitializationContext context = default)
  {
    this._formatProvider = formatProvider ?? throw new System.ArgumentNullException(nameof(formatProvider));
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  private IFormatProvider _formatProvider;
  protected virtual void OnConstructed(InitializationContext context = default)
  {
    Console.WriteLine("OnConstructed");
  }
}