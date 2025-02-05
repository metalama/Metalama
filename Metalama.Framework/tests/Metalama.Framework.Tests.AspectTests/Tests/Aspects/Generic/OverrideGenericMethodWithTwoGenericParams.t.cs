internal class TargetCode
{
  [Aspect]
  private T Method<T, S>(T a, S b)
  {
    return a;
  }
}