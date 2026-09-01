[ComparisonAttribute]
internal record DerivedRecord(int X, int Y) : BaseRecord(X)
{
  public virtual global::System.Boolean Equals(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug582_DerivedRecord.DerivedRecord? other)
  {
    // <target>
    global::System.Boolean result;
    result = (object)this == (object? )other || (base.Equals((global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug582_DerivedRecord.BaseRecord? )other) && global::System.Collections.Generic.EqualityComparer<global::System.Int32>.Default.Equals(this.Y, other!.Y));
    return (global::System.Boolean)result;
  }
}