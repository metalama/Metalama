public class TargetClass
{
  [Dependency]
  private readonly IFormatProvider _formatProvider;
  public TargetClass([AspectGenerated] IFormatProvider? formatProvider = null)
  {
    this._formatProvider = formatProvider ?? throw new System.ArgumentNullException(nameof(formatProvider));
  }
}
