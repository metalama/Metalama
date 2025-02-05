public class TargetClass
{
  [Dependency(IsLazy = true, IsRequired = false)]
  private IFormatProvider _formatProvider
  {
    get
    {
      return _formatProviderCache ??= (IFormatProvider)_serviceProvider.GetService(typeof(IFormatProvider));
    }
    init
    {
      throw new NotSupportedException("Cannot set '_formatProvider' because of the dependency aspect.");
    }
  }
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
}