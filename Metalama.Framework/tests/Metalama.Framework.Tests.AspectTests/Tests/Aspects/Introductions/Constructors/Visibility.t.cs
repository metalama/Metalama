[Introduction]
internal class TargetClass
{
  public TargetClass(global::System.Double a, global::System.String b = "Public")
  {
    global::System.Console.WriteLine("This is introduced constructor.");
  }
  protected TargetClass(global::System.Single a, global::System.String b = "Protected")
  {
    global::System.Console.WriteLine("This is introduced constructor.");
  }
  protected internal TargetClass(global::System.Int16 a, global::System.String b = "ProtectedInternal")
  {
    global::System.Console.WriteLine("This is introduced constructor.");
  }
  internal TargetClass(global::System.Int64 a, global::System.String b = "Internal")
  {
    global::System.Console.WriteLine("This is introduced constructor.");
  }
  private protected TargetClass(global::System.Int32 a, global::System.String b = "PrivateProtected")
  {
    global::System.Console.WriteLine("This is introduced constructor.");
  }
  private TargetClass(global::System.Byte a, global::System.String b = "Private")
  {
    global::System.Console.WriteLine("This is introduced constructor.");
  }
}