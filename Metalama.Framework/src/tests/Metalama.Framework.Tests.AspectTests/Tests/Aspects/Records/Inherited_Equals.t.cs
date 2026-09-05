internal class Targets
{
  [Override]
  internal record BaseRecord(int X)
  {
    public virtual global::System.Boolean Equals(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Records.Inherited_Equals.Targets.BaseRecord? other)
    {
      return (object)this == (object? )other || ((object? )other != null && this.EqualityContract == other.EqualityContract && global::System.Collections.Generic.EqualityComparer<global::System.Int32>.Default.Equals(this.X, other.X));
    }
  }
  internal record DerivedRecord(int X, int Y) : BaseRecord(X)
  {
    public virtual global::System.Boolean Equals(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Records.Inherited_Equals.Targets.DerivedRecord? other)
    {
      return (object)this == (object? )other || (base.Equals((global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Records.Inherited_Equals.Targets.BaseRecord? )other) && global::System.Collections.Generic.EqualityComparer<global::System.Int32>.Default.Equals(this.Y, other!.Y));
    }
  }
  internal record TwiceDerivedRecord(int X, int Y, int Z) : DerivedRecord(X, Y)
  {
    public virtual global::System.Boolean Equals(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Records.Inherited_Equals.Targets.TwiceDerivedRecord? other)
    {
      return (object)this == (object? )other || (base.Equals((global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Records.Inherited_Equals.Targets.DerivedRecord? )other) && global::System.Collections.Generic.EqualityComparer<global::System.Int32>.Default.Equals(this.Z, other!.Z));
    }
  }
}