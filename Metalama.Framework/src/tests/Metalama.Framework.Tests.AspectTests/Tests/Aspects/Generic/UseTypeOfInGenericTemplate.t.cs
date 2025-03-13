[Aspect]
internal class TargetCode
{
  public T GenericMethod<T>(T a)
  {
    global::System.Console.WriteLine(typeof(T).Name);
    return a;
  }
}