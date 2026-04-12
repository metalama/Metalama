public class BaseClass
{
  [TheAspect]
  public void Bar()
  {
    _logger.LogTrace("Starting BaseClass.Bar()");
  }
  [TheAspect]
  public void Bar2()
  {
    _logger.LogTrace("Starting BaseClass.Bar2()");
  }
  private ILogger _logger;
  public BaseClass([AspectGenerated] ILogger<BaseClass> logger = null)
  {
    this._logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
  }
}
public class DerivedClass : BaseClass
{
  [TheAspect]
  public void Foo()
  {
    _logger.LogTrace("Starting DerivedClass.Foo()");
  }
  private ILogger _logger;
  public DerivedClass([AspectGenerated] ILogger<DerivedClass> logger = null) : base(logger)
  {
    this._logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
  }
}