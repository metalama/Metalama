[Introduction]
internal class TargetClass
{
  public global::System.Int32 this[global::System.Int32 x]
  {
    get
    {
      global::System.Console.WriteLine("Introduced getter");
      return default(global::System.Int32);
    }
    private set
    {
      global::System.Console.WriteLine("Introduced setter");
    }
  }
}