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
}