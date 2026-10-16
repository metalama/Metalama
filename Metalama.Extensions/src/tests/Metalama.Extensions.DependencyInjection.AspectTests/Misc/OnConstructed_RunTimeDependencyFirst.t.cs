[DependencyAspect]
[InitializerAspect]
public class ImplicitConstructor
{
  private IFormatProvider _formatProvider;
  public ImplicitConstructor([AspectGenerated] InitializationContext context, [AspectGenerated] IFormatProvider? formatProvider = null)
  {
    this._formatProvider = formatProvider ?? throw new System.ArgumentNullException(nameof(formatProvider));
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
    Console.WriteLine("OnConstructed");
  }
}
[DependencyAspect]
[InitializerAspect]
public class ExplicitConstructors
{
  public ExplicitConstructors([AspectGenerated] InitializationContext context, [AspectGenerated] IFormatProvider? formatProvider = default)
  {
    this._formatProvider = formatProvider ?? throw new System.ArgumentNullException(nameof(formatProvider));
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  public ExplicitConstructors(int x, [AspectGenerated] InitializationContext context = default, [AspectGenerated] IFormatProvider? formatProvider = default)
  {
    this._formatProvider = formatProvider ?? throw new System.ArgumentNullException(nameof(formatProvider));
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  private IFormatProvider _formatProvider;
  public ExplicitConstructors([AspectGenerated] IFormatProvider? formatProvider = null) : this(context: default, formatProvider)
  {
  }
  protected virtual void OnConstructed(InitializationContext context = default)
  {
    Console.WriteLine("OnConstructed");
  }
}