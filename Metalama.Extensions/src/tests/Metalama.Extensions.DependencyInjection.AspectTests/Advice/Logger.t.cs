public class Foo
{
  [TheAspect]
  public void Bar()
  {
    _logger.LogTrace("Starting Foo.Bar()");
  }
  [TheAspect]
  public void Bar2()
  {
    _logger.LogTrace("Starting Foo.Bar2()");
  }
  private ILogger _logger;
  public Foo(ILogger<Foo> logger = null)
  {
    this._logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
  }
}