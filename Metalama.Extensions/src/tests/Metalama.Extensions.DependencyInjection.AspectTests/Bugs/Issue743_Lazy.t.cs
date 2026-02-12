public class TargetClass
{
  [Dependency(IsLazy = true)]
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
  private IFormatProvider? _formatProviderCache;
  private Func<IFormatProvider> _formatProviderFunc;
  public TargetClass([AspectGenerated] Func<IFormatProvider>? formatProvider = null)
  {
    this._formatProviderFunc = formatProvider ?? throw new System.ArgumentNullException(nameof(formatProvider));
  }
}