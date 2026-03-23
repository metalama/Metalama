public class TargetClass
{
  public void Method(int x)
  {
  }
  public void Method(string s)
  {
  }
  [DelegateAspect]
  public void Invoker()
  {
    // Delegate to Method(int)
    var intDelegate = new global::System.Action<global::System.Int32>(this.Method);
    // Delegate to Method(string)
    var stringDelegate = new global::System.Action<global::System.String>(this.Method);
    return;
  }
}
