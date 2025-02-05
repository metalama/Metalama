[IntroductionAspect, ReadAspect]
internal class C<T>
{
  public C()
  {
  }
  public void M()
  {
  }
  private C(T x)
  {
  }
  private T IntroducedMethod(T arg)
  {
    return default;
  }
  public void PrintBaseConstructors()
  {
    global::System.Console.WriteLine("object.Object()");
  }
  public void PrintBaseMethods()
  {
    global::System.Console.WriteLine("object.Equals(object?, object?)");
    global::System.Console.WriteLine("object.Equals(object?)");
    global::System.Console.WriteLine("object.GetHashCode()");
    global::System.Console.WriteLine("object.GetType()");
    global::System.Console.WriteLine("object.MemberwiseClone()");
    global::System.Console.WriteLine("object.ReferenceEquals(object?, object?)");
    global::System.Console.WriteLine("object.ToString()");
  }
}
[ReadAspect]
internal class D : C<int>
{
  public new void PrintBaseConstructors()
  {
    global::System.Console.WriteLine("C<int>.C()");
    global::System.Console.WriteLine("C<int>.C(int)");
  }
  public new void PrintBaseMethods()
  {
    global::System.Console.WriteLine("C<int>.IntroducedMethod(int)");
    global::System.Console.WriteLine("C<int>.M()");
    global::System.Console.WriteLine("C<int>.PrintBaseConstructors()");
    global::System.Console.WriteLine("C<int>.PrintBaseMethods()");
  }
}