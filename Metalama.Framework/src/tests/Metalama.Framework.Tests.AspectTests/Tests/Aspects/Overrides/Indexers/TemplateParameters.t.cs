[Test]
internal class TargetClass
{
  public int this[int x]
  {
    get
    {
      global::System.Console.WriteLine($"Override [{x}]");
      Console.WriteLine("Original");
      return x;
    }
    set
    {
      global::System.Console.WriteLine($"Override [{x}] {value}");
      Console.WriteLine("Original");
    }
  }
}