[Test]
internal class TargetClass
{
  public int this[int x]
  {
    get
    {
      global::System.Console.WriteLine("Override");
      Console.WriteLine("Original");
      return x;
    }
    set
    {
      Console.WriteLine("Original");
    }
  }
}
