internal class TargetClass
{
  [Override]
  public int Simple()
  {
    global::System.Int32 result;
    result = Foo();
    int Foo()
    {
      return 42;
    }
    global::System.Console.WriteLine("This is the overriding method.");
    return (global::System.Int32)result;
  }
  [Override]
  public int Simple_Static()
  {
    global::System.Int32 result;
    result = Foo();
    static int Foo()
    {
      return 42;
    }
    global::System.Console.WriteLine("This is the overriding method.");
    return (global::System.Int32)result;
  }
  [Override]
  public int ParameterCapture(int x)
  {
    global::System.Int32 result;
    result = Foo();
    int Foo()
    {
      return x + 1;
    }
    global::System.Console.WriteLine("This is the overriding method.");
    return (global::System.Int32)result;
  }
  [Override]
  public int LocalCapture(int x)
  {
    global::System.Int32 result;
    var y = x + 1;
    result = Foo();
    int Foo()
    {
      return y;
    }
    global::System.Console.WriteLine("This is the overriding method.");
    return (global::System.Int32)result;
  }
}