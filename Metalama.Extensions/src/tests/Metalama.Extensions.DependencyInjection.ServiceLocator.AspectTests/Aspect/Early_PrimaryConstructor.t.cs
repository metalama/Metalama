public class TargetClass
{
  [Dependency]
  private readonly ILogger? _logger;
  [Dependency]
  private IFormatProvider _formatProvider;
  public TargetClass(IFormatProvider formatProvider)
  {
    _formatProvider = (IFormatProvider)ServiceProviderProvider.ServiceProvider().GetService(typeof(IFormatProvider)) ?? throw new InvalidOperationException("The service 'IFormatProvider' could not be obtained from the service locator.");
    _logger = (ILogger? )ServiceProviderProvider.ServiceProvider().GetService(typeof(ILogger));
  }
}