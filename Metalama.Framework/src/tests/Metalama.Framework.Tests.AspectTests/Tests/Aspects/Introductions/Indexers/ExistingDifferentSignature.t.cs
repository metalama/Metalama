[Introduction]
internal class TargetClass
{
  public virtual int this[int x]
  {
    get
    {
      return 13;
    }
    set
    {
    }
  }
  public global::System.Int32 this[global::System.Int32 x, global::System.Int32 y]
  {
    get
    {
      global::System.Console.WriteLine($"This is introduced indexer {x} {y}.");
      return default(global::System.Int32);
    }
    set
    {
      global::System.Console.WriteLine($"This is introduced indexer {x} {y}.");
      return;
    }
  }
}