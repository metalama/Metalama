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
  public Func<ILogger>? logger { get; init; }
  public void Deconstruct(out Func<ILogger>? logger)
  {
    logger = this.logger;
  }
  public TargetRecordStruct(Func<ILogger>? logger = default)
  {
    this.logger = logger;
    this._loggerFunc = logger ?? throw new System.ArgumentNullException(nameof(logger));
  }
}