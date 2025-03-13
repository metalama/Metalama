[OverrideIndexer]
[OverrideProperty]
internal class TargetClass
{
  public TargetClass()
  {
    this[42] = 42;
    Foo = 42;
  }
  public int this[int x]
  {
    get
    {
      global::System.Console.WriteLine("Override");
      Console.WriteLine("Original");
      return 42;
    }
    init
    {
      Console.WriteLine("Original");
    }
  }
  public int Foo
  {
    get
    {
      return this[42];
    }
    init
    {
      this[42] = value;
    }
  }
}