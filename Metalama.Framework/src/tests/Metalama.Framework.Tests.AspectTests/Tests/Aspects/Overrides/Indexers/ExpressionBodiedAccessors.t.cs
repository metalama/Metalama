[Test]
internal class TargetClass
{
  public int this[int x]
  {
    get
    {
      global::System.Console.WriteLine("Override");
      return x;
    }
    set
    {
      global::System.Console.WriteLine("Override");
      Console.WriteLine("Original");
    }
  }
}
