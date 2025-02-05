public struct TargetStruct
{
  [Dependency(IsLazy = true)]
  private ILogger _logger
  {
    get
    {
      return _loggerCache ??= _loggerFunc.Invoke();
    }
    init
    {
      throw new NotSupportedException("Cannot set '_logger' because of the dependency aspect.");
    }
  }
  private ILogger? _loggerCache;
  private Func<ILogger> _loggerFunc;
  public TargetStruct(Func<ILogger>? logger = default)
  {
    this._loggerFunc = logger ?? throw new System.ArgumentNullException(nameof(logger));
  }
}