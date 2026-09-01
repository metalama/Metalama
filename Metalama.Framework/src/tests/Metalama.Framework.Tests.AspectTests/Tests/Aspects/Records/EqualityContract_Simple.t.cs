[Override]
internal record Target(int X)
{
  protected virtual global::System.Type EqualityContract
  {
    get
    {
      // <target>
      return typeof(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Records.EqualityContract_Simple.Target);
    }
  }
}