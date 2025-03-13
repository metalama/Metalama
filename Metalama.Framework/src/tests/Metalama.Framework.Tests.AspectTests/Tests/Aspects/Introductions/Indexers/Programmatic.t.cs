[Introduction]
internal class TargetClass
{
  public global::System.Object? this[global::System.Int32 x]
  {
    get
    {
      global::System.Console.WriteLine("Introduced");
      return default(global::System.Object? );
    }
    set
    {
      global::System.Console.WriteLine("Introduced");
    }
  }
}