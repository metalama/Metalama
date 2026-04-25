[MyAspect]
internal class TargetClass
{
  private IInternalService _service;
  public TargetClass(IInternalService? service = null)
  {
    this._service = service ?? throw new System.ArgumentNullException(nameof(service));
  }
}