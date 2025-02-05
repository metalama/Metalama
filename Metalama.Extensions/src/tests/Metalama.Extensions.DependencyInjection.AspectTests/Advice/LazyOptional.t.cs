[MyAspect]
public class TargetClass
{
  public TargetClass(Func<IFormatProvider>? formatProvider = default)
  {
    this._formatProviderFunc = formatProvider;
  }
  public TargetClass(int x, IFormatProvider existingParameter, Func<IFormatProvider>? formatProvider = default)
  {
    this._formatProviderFunc = formatProvider;
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