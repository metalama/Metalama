public class TargetClass
{
  [Dependency(IsLazy = true)]
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
  [Dependency(IsLazy = true)]
  private ILogger _logger
  {
    get
    {
      return _loggerCache ??= (ILogger)_serviceProvider.GetService(typeof(ILogger));
    }
    init
    {
      throw new NotSupportedException("Cannot set '_logger' because of the dependency aspect.");
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
  private ILogger? _loggerCache;
  private IServiceProvider _serviceProvider;
}