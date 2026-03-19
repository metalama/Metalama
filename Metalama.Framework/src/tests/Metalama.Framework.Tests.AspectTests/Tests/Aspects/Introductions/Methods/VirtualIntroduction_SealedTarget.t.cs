[ProgrammaticVirtualIntroduction]
[DeclarativeVirtualIntroduction]
internal sealed class SealedTargetClass
{
  public global::System.Int32 DeclarativeExplicitlyVirtualMethod()
  {
    global::System.Console.WriteLine("Introduced.");
    return (global::System.Int32)42;
  }
  public global::System.Int32 DeclarativeImplicitlyVirtualMethod()
  {
    global::System.Console.WriteLine("Introduced.");
    return (global::System.Int32)42;
  }
  public global::System.Int32 ExplicitlyVirtualMethod()
  {
    global::System.Console.WriteLine("Introduced.");
    return (global::System.Int32)42;
  }
  public global::System.Int32 ImplicitlyVirtualMethod()
  {
    global::System.Console.WriteLine("Introduced.");
    return (global::System.Int32)42;
  }
}
