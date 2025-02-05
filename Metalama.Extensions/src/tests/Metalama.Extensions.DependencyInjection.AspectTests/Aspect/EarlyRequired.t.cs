public class TargetClass
{
  [Dependency]
  private readonly IFormatProvider _formatProvider;
  public TargetClass(IFormatProvider? formatProvider = default)
  {
    this._formatProvider = formatProvider ?? throw new System.ArgumentNullException(nameof(formatProvider));
  }
  public TargetClass(int x, IFormatProvider existingParameter)
  {
    this._formatProvider = existingParameter ?? throw new System.ArgumentNullException(nameof(existingParameter));
  }
}