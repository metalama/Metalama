internal class TargetCode
{
  [Aspect]
  private T Method<T>(T a)
  {
    return a;
  }
}