[MyAspect]
public class TargetClass
{
  class GeneratedHelper
  {
    private IFormatProvider _formatProvider;
    public GeneratedHelper([AspectGenerated] IFormatProvider? formatProvider = default)
    {
      this._formatProvider = formatProvider ?? throw new System.ArgumentNullException(nameof(formatProvider));
    }
  }
}
