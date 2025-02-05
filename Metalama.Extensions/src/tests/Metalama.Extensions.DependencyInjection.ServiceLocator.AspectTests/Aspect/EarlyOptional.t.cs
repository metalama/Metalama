public class TargetClass
{
  [Dependency(IsRequired = false)]
  private readonly IFormatProvider? _formatProvider;
  public TargetClass()
  {
    _formatProvider = (IFormatProvider? )ServiceProviderProvider.ServiceProvider().GetService(typeof(IFormatProvider));
  }
  public TargetClass(int x, IFormatProvider existingParameter)
  {
    _formatProvider = (IFormatProvider? )ServiceProviderProvider.ServiceProvider().GetService(typeof(IFormatProvider));
  }
}