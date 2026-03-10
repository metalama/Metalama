[MyAspect]
internal class Target
{
  private void FuncTemplate(global::System.Func<global::System.Object> f)
  {
    f();
  }

  private void ActionTemplate(global::System.Action a)
  {
    a();
  }
}
