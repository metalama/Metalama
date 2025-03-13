public class Commerce
{
  [Inject]
  private IDisposable? _BillingProcessor;
  [Inject]
  private IDisposable? _CustomerProcessor;
  [Inject]
  private IDisposable? _Notifier;
  private readonly global::System.IServiceProvider? _serviceProvider;
}