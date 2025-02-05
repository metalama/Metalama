public partial class TargetClass
{
  [Dependency]
  private readonly IFormatProvider _formatProvider;
  public TargetClass()
  {
  }
  public TargetClass(int x, IFormatProvider existingParameter)
  {
  }
}