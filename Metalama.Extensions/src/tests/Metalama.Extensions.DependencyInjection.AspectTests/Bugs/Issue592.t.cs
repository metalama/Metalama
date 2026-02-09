[MyAspect]
public class TargetClass
{
  class GeneratedHelper
  {
    private IFormatProvider _formatProvider;
    public GeneratedHelper(IFormatProvider? formatProvider = null)
    {
      this._formatProvider = formatProvider ?? throw new System.ArgumentNullException(nameof(formatProvider));
    }
  }
}
