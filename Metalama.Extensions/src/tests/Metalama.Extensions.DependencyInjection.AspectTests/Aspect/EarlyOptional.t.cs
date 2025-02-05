public class TargetClass
{
  [Dependency(IsRequired = false)]
  private readonly IFormatProvider? _formatProvider;
  public TargetClass(IFormatProvider? formatProvider = default)
  {
    this._formatProvider = formatProvider;
  }
  public TargetClass(int x, IFormatProvider existingParameter)
  {
    this._formatProvider = existingParameter;
  }
}