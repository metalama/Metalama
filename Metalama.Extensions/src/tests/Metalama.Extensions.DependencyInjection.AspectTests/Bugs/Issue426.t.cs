public class ClientWeb
{
  public ClientWeb(object appSettings, bool throwOnError = true, [AspectGenerated] ILogger<ClientWeb> logger = default)
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
  protected ILogger<ScriptedClient> Logger;
  public ScriptedClient(object appSettings, ILogger<ScriptedClient> logger) : base(appSettings, logger: logger)
  {
    this._logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
    this.Logger = logger;
  }
  [ExtractedResultLoggingAspect]
  public void Bar()
  {
    throw new NotImplementedException();
  }
  private ILogger _logger;
}