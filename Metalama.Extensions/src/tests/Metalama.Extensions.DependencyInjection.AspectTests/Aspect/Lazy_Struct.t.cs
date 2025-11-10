// Error CS0191 on `this._loggerCache`: `A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)`
public readonly struct TargetStruct
{
  public int X { get; }
  [Dependency(IsLazy = true)]
  private ILogger _logger
  {
    get
    {
      return (ILogger)(this._loggerCache ??= _loggerFunc.Invoke());
    }
    init
    {
      throw new NotSupportedException("Cannot set '_logger' because of the dependency aspect.");
    }
  }
  private readonly ILogger? _loggerCache;
  private readonly Func<ILogger> _loggerFunc;
  public TargetStruct(int x, Func<ILogger>? logger = default)
  {
    X = x;
    this._loggerFunc = logger ?? throw new System.ArgumentNullException(nameof(logger));
  }
  public TargetStruct(Func<ILogger>? logger = null)
  {
    this._loggerFunc = logger ?? throw new System.ArgumentNullException(nameof(logger));
  }
}