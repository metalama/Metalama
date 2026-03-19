[ProgrammaticVirtualIntroduction]
[DeclarativeVirtualIntroduction]
internal sealed class SealedTargetClass
{
  public int DeclarativeExplicitlyVirtualProperty
  {
    get
    {
      global::System.Console.WriteLine("Introduced.");
      return 42;
    }
    set
    {
      global::System.Console.WriteLine("Introduced.");
    }
  }
  public int DeclarativeImplicitlyVirtualProperty
  {
    get
    {
      global::System.Console.WriteLine("Introduced.");
      return 42;
    }
    set
    {
      global::System.Console.WriteLine("Introduced.");
    }
  }
  public int ExplicitlyVirtualProperty
  {
    get
    {
      global::System.Console.WriteLine("Introduced.");
      return 42;
    }
    set
    {
      global::System.Console.WriteLine("Introduced.");
    }
  }
  public int ImplicitlyVirtualProperty
  {
    get
    {
      global::System.Console.WriteLine("Introduced.");
      return 42;
    }
    set
    {
      global::System.Console.WriteLine("Introduced.");
    }
  }
}
