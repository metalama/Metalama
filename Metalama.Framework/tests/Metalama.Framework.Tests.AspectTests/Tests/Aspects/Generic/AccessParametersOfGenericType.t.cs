internal class TargetCode
{
  [Aspect]
  private T Method<T, S>(T a, S b)
  {
    var v = a;
    var v_1 = b;
    return a;
  }
}