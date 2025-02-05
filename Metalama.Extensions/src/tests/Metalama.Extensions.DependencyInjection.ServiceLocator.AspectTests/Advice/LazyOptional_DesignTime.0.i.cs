namespace Metalama.Extensions.DependencyInjection.DotNet.Tests.Advice.LazyOptional_DesignTime
{
  partial class TargetClass
  {
    private IFormatProvider _formatProvider
    {
      get
      {
        return default;
      }
    }
    private IServiceProvider _serviceProvider;
    private IFormatProvider? _formatProviderCache;
  }
}