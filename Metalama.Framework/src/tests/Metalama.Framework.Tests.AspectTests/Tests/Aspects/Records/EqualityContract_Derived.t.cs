[Override]
internal record DerivedRecord(int X, int Y) : BaseRecord(X)
{
  protected override global::System.Type EqualityContract
  {
    get
    {
      // <target>
      return typeof(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Records.EqualityContract_Derived.DerivedRecord);
    }
  }
}