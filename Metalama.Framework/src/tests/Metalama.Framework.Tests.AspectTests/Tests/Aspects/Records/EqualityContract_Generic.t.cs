[Override]
internal record Target<T>(T Value)
{
  protected virtual global::System.Type EqualityContract
  {
    get
    {
      // <target>
      return typeof(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Records.EqualityContract_Generic.Target<T>);
    }
  }
}