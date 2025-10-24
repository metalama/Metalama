[MyAspect]
public class TargetClass
{
  public TargetClass([AspectGenerated] IFormatProvider? formatProvider = default)
  {
    this._formatProvider = formatProvider;
  }
  public TargetClass(int x, IFormatProvider existingParameter)
  {
    this._formatProvider = existingParameter;
  }
  private IFormatProvider? _formatProvider;
}