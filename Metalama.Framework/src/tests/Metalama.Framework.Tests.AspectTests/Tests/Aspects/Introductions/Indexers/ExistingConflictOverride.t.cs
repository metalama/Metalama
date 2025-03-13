[Introduction]
internal class TargetClass : BaseClass
{
  public virtual int this[int x, int y]
  {
    get
    {
      // Return a constant/do nothing.
      global::System.Console.WriteLine($"This is introduced indexer {x}.");
      return 27;
    }
    set
    {
      // Return a constant/do nothing.
      global::System.Console.WriteLine($"This is introduced indexer {x}.");
      return;
    }
  }
  public override global::System.Int32 this[global::System.Int32 x]
  {
    get
    {
      // Call the base indexer.
      global::System.Console.WriteLine($"This is introduced indexer {x}.");
      return base[x];
    }
    set
    {
      // Call the base indexer.
      global::System.Console.WriteLine($"This is introduced indexer {x}.");
      base[x] = value;
      return;
    }
  }
  public global::System.Int32 this[global::System.Int32 x, global::System.Int32 y, global::System.Int32 z]
  {
    get
    {
      // Return default value/do nothing.
      global::System.Console.WriteLine($"This is introduced indexer {x}.");
      return default(global::System.Int32);
    }
    set
    {
      // Return default value/do nothing.
      global::System.Console.WriteLine($"This is introduced indexer {x}.");
      return;
    }
  }
}