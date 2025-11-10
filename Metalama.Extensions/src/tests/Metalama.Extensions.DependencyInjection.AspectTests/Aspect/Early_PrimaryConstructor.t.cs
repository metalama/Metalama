public class TargetClass
{
  [Dependency]
  private readonly ILogger _logger;
  [Dependency]
  private IFormatProvider _formatProvider;
  public TargetClass(IFormatProvider formatProvider, [AspectGenerated] ILogger? logger = default)
  {
    this._logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
    this._formatProvider = formatProvider ?? throw new System.ArgumentNullException(nameof(formatProvider));
  }
}