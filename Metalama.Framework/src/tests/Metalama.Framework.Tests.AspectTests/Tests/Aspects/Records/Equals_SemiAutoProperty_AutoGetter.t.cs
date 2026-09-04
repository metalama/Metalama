[Override]
internal record Transformed
{
  public int Validated
  {
    get;
    set
    {
      if (value < 0)
      {
        throw new ArgumentOutOfRangeException(nameof(value));
      }
      field = value;
    }
  }
  public int Read { get => field; set => field = value; }
  public virtual global::System.Boolean Equals(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Records.Equals_SemiAutoProperty_AutoGetter.Transformed? other)
  {
    // <target>
    return (object)this == (object? )other || ((object? )other != null && this.EqualityContract == other.EqualityContract && global::System.Collections.Generic.EqualityComparer<global::System.Int32>.Default.Equals(this.Validated, other.Validated) && global::System.Collections.Generic.EqualityComparer<global::System.Int32>.Default.Equals(this.Read, other.Read));
  }
}