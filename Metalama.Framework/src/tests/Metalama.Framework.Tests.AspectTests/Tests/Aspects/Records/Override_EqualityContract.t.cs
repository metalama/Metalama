[Override]
internal record Target(int X)
{
  protected virtual global::System.Type EqualityContract
  {
    get
    {
      // <target>
      global::System.Console.WriteLine("Overridden!");
      return typeof(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Records.Override_EqualityContract.Target);
    }
  }
}