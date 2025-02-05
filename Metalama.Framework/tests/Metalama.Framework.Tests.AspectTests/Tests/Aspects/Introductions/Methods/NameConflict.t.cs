[Introduction]
internal class TargetClass
{
  public global::System.Object Method_GenericParameterConflict<TParameter>()
  {
    // Forces conflict between the method type parameter name and name of the local variable.
    global::System.Object TParameter_1;
    TParameter_1 = default(global::System.Object);
    global::System.Console.WriteLine("This is introduced method.");
    return (global::System.Object)TParameter_1;
  }
  public global::System.Int32 Method_NameConflict(global::System.Int32 p)
  {
    // Forces conflict between the method name (which is different than template name) and a local function.
    // If the local function is not renamed, an error is produced due to different return type.
    global::System.Int32 x;
    x = default(global::System.Int32);
    global::System.Console.WriteLine("This is introduced method.");
    if (p > 0)
    {
      return (global::System.Int32)Method_NameConflict(p - 1);
    }
    Method_NameConflict_1();
    return p;
    void Method_NameConflict_1()
    {
    }
  }
  public global::System.Object Method_ParameterConflict(global::System.Int32 x)
  {
    // Forces conflict between the method parameter name and name of the local variable.
    global::System.Object x_1;
    x_1 = default(global::System.Object);
    global::System.Console.WriteLine("This is introduced method.");
    return (global::System.Object)x_1;
  }
}