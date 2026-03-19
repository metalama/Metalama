public class TargetClass
{
  public void GenericMethod<T>(T value)
  {
  }
  [DelegateAspect]
  public void Invoker()
  {
    var action = this.GenericMethod<global::System.Int32>;
    return;
  }
}
