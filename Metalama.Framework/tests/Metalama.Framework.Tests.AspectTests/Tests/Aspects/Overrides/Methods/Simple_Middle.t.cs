internal class TargetClass
{
  [Override]
  public void TargetMethod_Void()
  {
    global::System.Console.WriteLine("This is the overriding method.");
    Console.WriteLine("This is the original method.");
    object x = null;
    global::System.Console.WriteLine("This is the overriding method.");
    return;
  }
  [Override]
  public void TargetMethod_Void(int x, int y)
  {
    global::System.Console.WriteLine("This is the overriding method.");
    Console.WriteLine($"This is the original method {x} {y}.");
    object x_1 = null;
    global::System.Console.WriteLine("This is the overriding method.");
    return;
  }
  [Override]
  public int TargetMethod_Int()
  {
    global::System.Console.WriteLine("This is the overriding method.");
    global::System.Int32 x;
    Console.WriteLine("This is the original method.");
    x = 42;
    global::System.Console.WriteLine("This is the overriding method.");
    return (global::System.Int32)x;
  }
  [Override]
  public int TargetMethod_Int(int x, int y)
  {
    global::System.Console.WriteLine("This is the overriding method.");
    global::System.Int32 x_1;
    Console.WriteLine($"This is the original method {x} {y}.");
    x_1 = x + y;
    global::System.Console.WriteLine("This is the overriding method.");
    return (global::System.Int32)x_1;
  }
  [Override]
  public static void TargetMethod_Static()
  {
    global::System.Console.WriteLine("This is the overriding method.");
    Console.WriteLine("This is the original static method.");
    object x = null;
    global::System.Console.WriteLine("This is the overriding method.");
    return;
  }
  [Override]
  public void TargetMethod_Out(out int x)
  {
    global::System.Console.WriteLine("This is the overriding method.");
    Console.WriteLine("This is the original method.");
    x = 42;
    object x_1 = null;
    global::System.Console.WriteLine("This is the overriding method.");
    return;
  }
  [Override]
  public void TargetMethod_Ref(ref int x)
  {
    global::System.Console.WriteLine("This is the overriding method.");
    Console.WriteLine($"This is the original method {x}.");
    x = 42;
    object x_1 = null;
    global::System.Console.WriteLine("This is the overriding method.");
    return;
  }
  [Override]
  public void TargetMethod_In(in DateTime x)
  {
    global::System.Console.WriteLine("This is the overriding method.");
    Console.WriteLine($"This is the original method {x}.");
    object x_1 = null;
    global::System.Console.WriteLine("This is the overriding method.");
    return;
  }
}