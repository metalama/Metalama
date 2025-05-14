public class ClientWeb
{
  public ClientWeb(object appSettings, bool throwOnError = true, ILogger<ClientWeb> logger = default)
  {
    this._logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
  }
  [ExtractedResultLoggingAspect]
  public void Foo()
  {
    throw new NotImplementedException();
  }
  private ILogger _logger;
}
public class ScriptedClient : ClientWeb
{
  protected ILogger<ScriptedClient> _logger;
  public ScriptedClient(object appSettings, ILogger<ScriptedClient> logger) : base(appSettings, logger: logger)
  {
    _logger = logger;
  }
  [ExtractedResultLoggingAspect]
  public void Bar()
  {
    throw new NotImplementedException();
  }
}