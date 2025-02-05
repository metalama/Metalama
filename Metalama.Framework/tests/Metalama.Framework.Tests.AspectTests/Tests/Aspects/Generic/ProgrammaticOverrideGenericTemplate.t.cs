internal class TargetCode
{
  [Aspect]
  private T Method<T>(T a)
  {
    global::System.Console.WriteLine(a);
    return (T)this.Method_Source<T>(a)!;
  }
  private T Method_Source<T>(T a)
  {
    return a;
  }
}