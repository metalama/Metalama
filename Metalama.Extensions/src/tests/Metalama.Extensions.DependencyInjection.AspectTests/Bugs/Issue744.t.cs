// Error LAMA0704 on `TargetClass`: `The dependency 'IInternalService' cannot be pulled from the constructor 'TargetClass()' because 'IInternalService' has lower accessibility than the constructor.`
[MyAspect]
public class TargetClass
{
  public TargetClass() { }
}
