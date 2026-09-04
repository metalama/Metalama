internal class Targets
{
  [Override]
  internal record BaseRecord(int X)
  {
    public override global::System.Int32 GetHashCode()
    {
      global::System.Console.WriteLine("Overridden!");
      return unchecked(((global::System.Collections.Generic.EqualityComparer<global::System.Type>.Default.GetHashCode(this.EqualityContract)) * -1521134295) + global::System.Collections.Generic.EqualityComparer<global::System.Int32>.Default.GetHashCode(this.X));
    }
  }
  internal record DerivedRecord(int X, int Y) : BaseRecord(X)
  {
    public override global::System.Int32 GetHashCode()
    {
      global::System.Console.WriteLine("Overridden!");
      return unchecked(((base.GetHashCode()) * -1521134295) + global::System.Collections.Generic.EqualityComparer<global::System.Int32>.Default.GetHashCode(this.Y));
    }
  }
  internal record TwiceDerivedRecord(int X, int Y, int Z) : DerivedRecord(X, Y)
  {
    public override global::System.Int32 GetHashCode()
    {
      global::System.Console.WriteLine("Overridden!");
      return unchecked(((base.GetHashCode()) * -1521134295) + global::System.Collections.Generic.EqualityComparer<global::System.Int32>.Default.GetHashCode(this.Z));
    }
  }
}