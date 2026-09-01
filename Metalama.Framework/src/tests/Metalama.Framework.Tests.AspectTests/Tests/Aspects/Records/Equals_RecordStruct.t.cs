[Override]
internal record struct Target(int X, string Y)
{
  public readonly global::System.Boolean Equals(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Records.Equals_RecordStruct.Target other)
  {
    // <target>
    return global::System.Collections.Generic.EqualityComparer<global::System.Int32>.Default.Equals(this.X, other.X) && global::System.Collections.Generic.EqualityComparer<global::System.String>.Default.Equals(this.Y, other.Y);
  }
}