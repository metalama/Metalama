internal class TargetCode
{
  private int Method1(int a) => a;
  [ExcludeAspect(typeof(Aspect))]
  private string Method2(string s) => s;
}