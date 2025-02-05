public partial class TargetClass
{
  [Dependency(IsRequired = false)]
  private readonly IFormatProvider? _formatProvider;
  public TargetClass()
  {
  }
  public TargetClass(int x, IFormatProvider existingParameter)
  {
  }
}