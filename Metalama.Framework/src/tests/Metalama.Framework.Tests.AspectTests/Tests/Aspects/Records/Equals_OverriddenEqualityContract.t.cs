[Override]
internal record Target(int X)
{
  public virtual global::System.Boolean Equals(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Records.Equals_OverriddenEqualityContract.Target? other)
  {
    // <target>
    return (object)this == (object? )other || ((object? )other != null && this.EqualityContract == other.EqualityContract && global::System.Collections.Generic.EqualityComparer<global::System.Int32>.Default.Equals(this.X, other.X));
  }
  protected virtual global::System.Type EqualityContract
  {
    get
    {
      // <target>
      global::System.Console.WriteLine("  (the getter of EqualityContract runs)");
      return typeof(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Records.Equals_OverriddenEqualityContract.Target);
    }
  }
}