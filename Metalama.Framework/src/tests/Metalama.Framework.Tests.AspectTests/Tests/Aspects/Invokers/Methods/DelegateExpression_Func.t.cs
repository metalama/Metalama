public class TargetClass
{
  public string Convert(int x)
  {
    return x.ToString();
  }
  public string Convert(string s)
  {
    return s;
  }
  [DelegateAspect]
  public void Invoker()
  {
    var func = new global::System.Func<global::System.Int32, global::System.String>(this.Convert);
    return;
  }
}
