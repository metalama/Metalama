internal class TargetCode
{
  [Aspect]
  private T Method<T, S>(T a, S b)
  {
    var v = default(T);
    var t = typeof(T);
    var v_1 = default(S);
    var t_1 = typeof(S);
    return a;
  }
}