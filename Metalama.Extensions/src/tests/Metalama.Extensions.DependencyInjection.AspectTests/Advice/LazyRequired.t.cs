[MyAspect]
public class TargetClass
{
  public TargetClass([AspectGenerated] Func<IFormatProvider>? formatProvider = default)
  {
    this._formatProviderFunc = formatProvider ?? throw new System.ArgumentNullException(nameof(formatProvider));
  }
  public TargetClass(int x, IFormatProvider existingParameter, [AspectGenerated] Func<IFormatProvider>? formatProvider = default)
  {
    this._formatProviderFunc = formatProvider ?? throw new System.ArgumentNullException(nameof(formatProvider));
  }
  private IFormatProvider? _formatProviderCache;
  private Func<IFormatProvider> _formatProviderFunc;
  private IFormatProvider _formatProvider
  {
    get
    {
      return _formatProviderCache ??= _formatProviderFunc.Invoke();
    }
  }
}