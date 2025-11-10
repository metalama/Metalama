public record struct TargetRecordStruct
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
  public int X { get; init; }
  public Func<ILogger>? logger { get; init; }
  public void Deconstruct(out int X, out Func<ILogger>? logger)
  {
    X = this.X;
    logger = this.logger;
  }
  public TargetRecordStruct(int X, Func<ILogger>? logger = default)
  {
    this.X = X;
    this.logger = logger;
    this._loggerFunc = logger ?? throw new System.ArgumentNullException(nameof(logger));
  }
  public TargetRecordStruct(Func<ILogger>? logger = null)
  {
    this._loggerFunc = logger ?? throw new System.ArgumentNullException(nameof(logger));
  }
}