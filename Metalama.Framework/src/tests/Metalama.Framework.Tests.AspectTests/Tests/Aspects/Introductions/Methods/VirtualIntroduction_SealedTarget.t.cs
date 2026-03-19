[ProgrammaticVirtualIntroduction]
[DeclarativeVirtualIntroduction]
internal sealed class SealedTargetClass
{
  public int DeclarativeExplicitlyVirtualMethod()
  {
    global::System.Console.WriteLine("Introduced.");
    return 42;
  }
  public int DeclarativeImplicitlyVirtualMethod()
  {
    global::System.Console.WriteLine("Introduced.");
    return 42;
  }
  public int ExplicitlyVirtualMethod()
  {
    global::System.Console.WriteLine("Introduced.");
    return 42;
  }
  public int ImplicitlyVirtualMethod()
  {
    global::System.Console.WriteLine("Introduced.");
    return 42;
  }
}
