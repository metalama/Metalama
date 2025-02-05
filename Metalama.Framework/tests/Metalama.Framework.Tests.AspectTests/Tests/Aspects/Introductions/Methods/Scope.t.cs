[Introduction]
internal class TargetClass
{
  public global::System.Int32 DefaultScope()
  {
    global::System.Console.WriteLine("This is introduced method.");
    return (global::System.Int32)42;
  }
  public static global::System.Int32 DefaultScopeStatic()
  {
    global::System.Console.WriteLine("This is introduced method.");
    return (global::System.Int32)42;
  }
  public global::System.Int32 InstanceScope()
  {
    global::System.Console.WriteLine("This is introduced method.");
    return (global::System.Int32)42;
  }
  public global::System.Int32 InstanceScopeStatic()
  {
    global::System.Console.WriteLine("This is introduced method.");
    return (global::System.Int32)42;
  }
  public static global::System.Int32 StaticScope()
  {
    global::System.Console.WriteLine("This is introduced method.");
    return (global::System.Int32)42;
  }
  public static global::System.Int32 StaticScopeStatic()
  {
    global::System.Console.WriteLine("This is introduced method.");
    return (global::System.Int32)42;
  }
  public global::System.Int32 TargetScope()
  {
    global::System.Console.WriteLine("This is introduced method.");
    return (global::System.Int32)42;
  }
  public global::System.Int32 TargetScopeStatic()
  {
    global::System.Console.WriteLine("This is introduced method.");
    return (global::System.Int32)42;
  }
}