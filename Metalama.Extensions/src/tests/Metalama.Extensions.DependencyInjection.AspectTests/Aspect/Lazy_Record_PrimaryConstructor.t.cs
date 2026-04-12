public record TargetRecord
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
  public TargetRecord(int X, [AspectGenerated] Func<ILogger>? logger = default)
  {
    this.X = X;
    this._loggerFunc = logger ?? throw new System.ArgumentNullException(nameof(logger));
  }
}