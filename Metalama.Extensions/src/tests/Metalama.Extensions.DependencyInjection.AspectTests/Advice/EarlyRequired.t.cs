[MyAspect]
public class TargetClass
{
  public TargetClass(IFormatProvider? formatProvider = default)
  {
    this._formatProvider = formatProvider ?? throw new System.ArgumentNullException(nameof(formatProvider));
  }
  public TargetClass(int x, IFormatProvider existingParameter)
  {
    this._formatProvider = existingParameter ?? throw new System.ArgumentNullException(nameof(existingParameter));
  }
  private IFormatProvider _formatProvider;
}