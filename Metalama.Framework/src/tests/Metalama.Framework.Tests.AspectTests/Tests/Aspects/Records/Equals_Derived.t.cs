[Override]
internal record DerivedRecord(int X, int Y) : BaseRecord(X)
{
  public virtual global::System.Boolean Equals(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Records.Equals_Derived.DerivedRecord? other)
  {
    // <target>
    return (object)this == (object? )other || (base.Equals((global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Records.Equals_Derived.BaseRecord? )other) && global::System.Collections.Generic.EqualityComparer<global::System.Int32>.Default.Equals(this.Y, other!.Y));
  }
}