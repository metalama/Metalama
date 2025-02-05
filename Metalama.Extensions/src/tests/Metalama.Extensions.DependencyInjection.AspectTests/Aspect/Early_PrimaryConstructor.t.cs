public class TargetClass
{
  [Dependency]
  private readonly ILogger _logger;
  [Dependency]
  private IFormatProvider formatProvider;
  public TargetClass(IFormatProvider formatProvider, ILogger? logger = default)
  {
    this._logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
    this.formatProvider = formatProvider ?? throw new System.ArgumentNullException(nameof(formatProvider));
  }
}