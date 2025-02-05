public partial class TargetClass
{
  [Dependency(IsLazy = true, IsRequired = false)]
  private readonly IFormatProvider _formatProvider;
  public TargetClass()
  {
  }
  public TargetClass(int x, IFormatProvider existingParameter)
  {
  }
}