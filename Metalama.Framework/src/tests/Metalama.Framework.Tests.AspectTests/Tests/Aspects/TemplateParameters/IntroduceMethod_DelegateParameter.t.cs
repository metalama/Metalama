[MyAspect]
internal class Target
{
  private void ActionTemplate(global::System.Action a)
  {
    a();
  }
  private void FuncTemplate(global::System.Func<global::System.Object> f)
  {
    f();
  }
}
