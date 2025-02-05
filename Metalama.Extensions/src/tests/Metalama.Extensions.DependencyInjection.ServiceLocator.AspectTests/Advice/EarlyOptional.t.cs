[MyAspect]
public class TargetClass
{
  public TargetClass()
  {
    _formatProvider = (IFormatProvider? )ServiceProviderProvider.ServiceProvider().GetService(typeof(IFormatProvider));
  }
  public TargetClass(int x, IFormatProvider existingParameter)
  {
    _formatProvider = (IFormatProvider? )ServiceProviderProvider.ServiceProvider().GetService(typeof(IFormatProvider));
  }
  private IFormatProvider? _formatProvider;
}