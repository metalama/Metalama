[Override]
internal record Target
{
  public virtual global::System.Boolean Equals(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Records.Equals_Empty.Target? other)
  {
    // <target>
    return (object)this == (object? )other || ((object? )other != null && this.EqualityContract == other.EqualityContract);
  }
}