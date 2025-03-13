[Test]
internal class TargetClass
{
  public int this[int x]
  {
    get
    {
      Console.WriteLine("Original");
      return x;
    }
    set
    {
      global::System.Console.WriteLine("Override");
      Console.WriteLine("Original");
    }
  }
}
