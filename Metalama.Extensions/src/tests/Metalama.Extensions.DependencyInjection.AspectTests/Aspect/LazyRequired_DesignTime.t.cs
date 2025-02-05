public partial class TargetClass
{
  [Dependency(IsLazy = true)]
  private readonly IFormatProvider _formatProvider;
  public TargetClass()
  {
  }
  public TargetClass(int x, IFormatProvider existingParameter)
  {
  }
}