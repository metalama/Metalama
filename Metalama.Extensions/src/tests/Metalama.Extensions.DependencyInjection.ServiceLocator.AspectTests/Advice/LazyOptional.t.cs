[MyAspect]
public class TargetClass
{
  public TargetClass()
  {
    _serviceProvider = ServiceProviderProvider.ServiceProvider();
  }
  public TargetClass(int x, IFormatProvider existingParameter)
  {
    _serviceProvider = ServiceProviderProvider.ServiceProvider();
  }
  private IFormatProvider? _formatProviderCache;
  private IServiceProvider _serviceProvider;
  private IFormatProvider _formatProvider
  {
    get
    {
      return _formatProviderCache ??= (IFormatProvider)_serviceProvider.GetService(typeof(IFormatProvider));
    }
  }
}