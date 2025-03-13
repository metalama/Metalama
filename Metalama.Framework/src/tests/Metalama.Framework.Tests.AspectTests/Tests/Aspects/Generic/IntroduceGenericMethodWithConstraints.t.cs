[Aspect]
internal class TargetCode
{
  public T GenericMethod<T>(T a)
    where T : notnull, global::System.IDisposable, new()
  {
    return a;
  }
}