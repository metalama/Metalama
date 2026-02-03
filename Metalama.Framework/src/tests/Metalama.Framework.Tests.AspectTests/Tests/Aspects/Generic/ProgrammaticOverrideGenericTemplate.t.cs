internal class TargetCode
{
  [Aspect]
  private T Method<T>(T a)
  {
    global::System.Console.WriteLine(a);
    return a;
  }
}
