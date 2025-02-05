public class TargetClass
{
  [Dependency(IsLazy = true, IsRequired = false)]
  private IFormatProvider _formatProvider
  {
    get
    {
      return _formatProviderCache ??= _formatProviderFunc.Invoke();
    }
    init
    {
      throw new NotSupportedException("Cannot set '_formatProvider' because of the dependency aspect.");
    }
  }
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
}