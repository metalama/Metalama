[ComparisonAttribute]
internal record Target(int X, string Y)
{
  public virtual global::System.Boolean Equals(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug582_PositionalRecord.Target? other)
  {
    // <target>
    global::System.Boolean result;
    result = (object)this == (object? )other || ((object? )other != null && this.EqualityContract == other.EqualityContract && global::System.Collections.Generic.EqualityComparer<global::System.Int32>.Default.Equals(this.X, other.X) && global::System.Collections.Generic.EqualityComparer<global::System.String>.Default.Equals(this.Y, other.Y));
    return (global::System.Boolean)result;
  }
}