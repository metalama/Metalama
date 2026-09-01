[Override]
internal record Target
{
  public int Field;
  public string? Property { get; set; }
  public virtual global::System.Boolean Equals(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Records.Equals_Fields.Target? other)
  {
    // <target>
    return (object)this == (object? )other || ((object? )other != null && this.EqualityContract == other.EqualityContract && global::System.Collections.Generic.EqualityComparer<global::System.Int32>.Default.Equals(this.Field, other.Field) && global::System.Collections.Generic.EqualityComparer<global::System.String?>.Default.Equals(this.Property, other.Property));
  }
}