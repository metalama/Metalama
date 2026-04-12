public record struct TargetRecordStruct
{
  public void Deconstruct(out int X)
  {
    X = this.X;
  }
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
  public int X { get; init; }
  public TargetRecordStruct(int X, Func<ILogger>? logger = default)
  {
    this.X = X;
    this._loggerFunc = logger ?? throw new System.ArgumentNullException(nameof(logger));
  }
  public TargetRecordStruct(Func<ILogger>? logger = null)
  {
    this._loggerFunc = logger ?? throw new System.ArgumentNullException(nameof(logger));
  }
}