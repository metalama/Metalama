[MyAspect]
internal class TargetClass
{
  public TargetClass(IInternalService? service = default)
  {
    this._service = service ?? throw new System.ArgumentNullException(nameof(service));
  }
  private IInternalService _service;
}