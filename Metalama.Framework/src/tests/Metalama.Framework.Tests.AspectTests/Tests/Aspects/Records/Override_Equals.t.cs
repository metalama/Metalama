[Override]
internal record Target(int X)
{
  public virtual global::System.Boolean Equals(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Records.Override_Equals.Target? other)
  {
    // <target>
    global::System.Console.WriteLine("Overridden!");
    return (object)this == (object? )other || ((object? )other != null && this.EqualityContract == other.EqualityContract && global::System.Collections.Generic.EqualityComparer<global::System.Int32>.Default.Equals(this.X, other.X));
  }
}