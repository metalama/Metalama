[Introduction]
internal class TargetClass
{
  internal global::System.Int32 Internal()
  {
    global::System.Console.WriteLine("This is introduced method.");
    return (global::System.Int32)42;
  }
  private global::System.Int32 Private()
  {
    global::System.Console.WriteLine("This is introduced method.");
    return (global::System.Int32)42;
  }
  private protected global::System.Int32 PrivateProtected()
  {
    global::System.Console.WriteLine("This is introduced method.");
    return (global::System.Int32)42;
  }
  protected global::System.Int32 Protected()
  {
    global::System.Console.WriteLine("This is introduced method.");
    return (global::System.Int32)42;
  }
  protected internal global::System.Int32 ProtectedInternal()
  {
    global::System.Console.WriteLine("This is introduced method.");
    return (global::System.Int32)42;
  }
  public global::System.Int32 Public()
  {
    global::System.Console.WriteLine("This is introduced method.");
    return (global::System.Int32)42;
  }
}