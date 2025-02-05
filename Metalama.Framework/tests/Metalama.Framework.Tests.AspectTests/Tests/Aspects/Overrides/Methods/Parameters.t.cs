[IntroduceAspect]
internal class TargetClass
{
  [Override]
  public void VoidNoParameters()
  {
    Console.WriteLine("This is the original method.");
    object result = null;
    global::System.Console.WriteLine("This is the override method.");
    global::System.Console.WriteLine("Returns void");
    return;
  }
  [Override]
  public void VoidTwoParameters(int x, int y)
  {
    Console.WriteLine("This is the original method.");
    object result = null;
    global::System.Console.WriteLine("This is the override method.");
    global::System.Console.WriteLine($"Param x = {x}");
    global::System.Console.WriteLine($"Param y = {y}");
    global::System.Console.WriteLine("Returns void");
    return;
  }
  public void VoidGeneric<T>(T param)
  {
    Console.WriteLine("This is the original method.");
    object result = null;
    global::System.Console.WriteLine("This is the override method.");
    global::System.Console.WriteLine($"Param param = {param}");
    global::System.Console.WriteLine("Returns void");
    return;
  }
  [Override]
  public int IntNoParameters()
  {
    global::System.Int32 result;
    Console.WriteLine("This is the original method.");
    result = 42;
    global::System.Console.WriteLine("This is the override method.");
    global::System.Console.WriteLine("Returns int");
    return (global::System.Int32)result;
  }
  [Override]
  public object ObjectNoParameters()
  {
    global::System.Object result;
    Console.WriteLine("This is the original method.");
    result = new object ();
    global::System.Console.WriteLine("This is the override method.");
    global::System.Console.WriteLine("Returns object");
    return (global::System.Object)result;
  }
  public T Generic<T>(T param)
  {
    T result;
    Console.WriteLine("This is the original method.");
    result = param;
    global::System.Console.WriteLine("This is the override method.");
    global::System.Console.WriteLine($"Param param = {param}");
    global::System.Console.WriteLine("Returns TargetClass.Generic<T>(T)/T");
    return (T)result;
  }
  [Override]
  public void OutParameter(out int x)
  {
    Console.WriteLine("This is the original method.");
    x = 42;
    object result = null;
    global::System.Console.WriteLine("This is the override method.");
    global::System.Console.WriteLine($"Param x = {x}");
    global::System.Console.WriteLine("Returns void");
    return;
  }
  [Override]
  public void RefParameter(ref int x)
  {
    Console.WriteLine("This is the original method.");
    x = 42;
    object result = null;
    global::System.Console.WriteLine("This is the override method.");
    global::System.Console.WriteLine($"Param x = {x}");
    global::System.Console.WriteLine("Returns void");
    return;
  }
  [Override]
  public void InParameter(in DateTime x)
  {
    Console.WriteLine("This is the original method.");
    object result = null;
    global::System.Console.WriteLine("This is the override method.");
    global::System.Console.WriteLine($"Param x = {x}");
    global::System.Console.WriteLine("Returns void");
    return;
  }
  public T IntroducedGeneric<T>(T param)
  {
    T result;
    global::System.Console.WriteLine("This is the introduced method.");
    result = param;
    global::System.Console.WriteLine("This is the override method.");
    global::System.Console.WriteLine($"Param param = {param}");
    global::System.Console.WriteLine("Returns IntroduceAspectAttribute.IntroducedGeneric<T>(T)/T");
    return (T)result;
  }
  public void IntroducedInParameter(in global::System.DateTime x)
  {
    global::System.Console.WriteLine("This is the introduced method.");
    object result = null;
    global::System.Console.WriteLine("This is the override method.");
    global::System.Console.WriteLine($"Param x = {x}");
    global::System.Console.WriteLine("Returns void");
    return;
  }
  public global::System.Int32 IntroducedIntNoParameters()
  {
    global::System.Int32 result;
    global::System.Console.WriteLine("This is the introduced method.");
    result = (global::System.Int32)42;
    global::System.Console.WriteLine("This is the override method.");
    global::System.Console.WriteLine("Returns int");
    return (global::System.Int32)result;
  }
  public global::System.Object IntroducedObjectNoParameters()
  {
    global::System.Object result;
    global::System.Console.WriteLine("This is the introduced method.");
    result = (global::System.Object)new object ();
    global::System.Console.WriteLine("This is the override method.");
    global::System.Console.WriteLine("Returns object");
    return (global::System.Object)result;
  }
  public void IntroducedOutParameter(out global::System.Int32 x)
  {
    global::System.Console.WriteLine("This is the introduced method.");
    x = 42;
    object result = null;
    global::System.Console.WriteLine("This is the override method.");
    global::System.Console.WriteLine($"Param x = {x}");
    global::System.Console.WriteLine("Returns void");
    return;
  }
  public void IntroducedRefParameter(ref global::System.Int32 x)
  {
    global::System.Console.WriteLine("This is the introduced method.");
    x = 42;
    object result = null;
    global::System.Console.WriteLine("This is the override method.");
    global::System.Console.WriteLine($"Param x = {x}");
    global::System.Console.WriteLine("Returns void");
    return;
  }
  public void IntroducedVoidGeneric<T>(T param)
  {
    global::System.Console.WriteLine("This is the introduced method.");
    object result = null;
    global::System.Console.WriteLine("This is the override method.");
    global::System.Console.WriteLine($"Param param = {param}");
    global::System.Console.WriteLine("Returns void");
    return;
  }
  public void IntroducedVoidNoParameters()
  {
    global::System.Console.WriteLine("This is the original method.");
    object result = null;
    global::System.Console.WriteLine("This is the override method.");
    global::System.Console.WriteLine("Returns void");
    return;
  }
  public void IntroducedVoidTwoParameters(global::System.Int32 x, global::System.Int32 y)
  {
    global::System.Console.WriteLine("This is the introduced method.");
    object result = null;
    global::System.Console.WriteLine("This is the override method.");
    global::System.Console.WriteLine($"Param x = {x}");
    global::System.Console.WriteLine($"Param y = {y}");
    global::System.Console.WriteLine("Returns void");
    return;
  }
}