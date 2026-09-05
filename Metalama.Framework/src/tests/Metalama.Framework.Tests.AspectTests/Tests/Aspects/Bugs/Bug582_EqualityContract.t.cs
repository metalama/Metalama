[OverrideEqualityContractAttribute]
internal record Target
{
  protected virtual global::System.Type EqualityContract
  {
    get
    {
      // <target>
      global::System.Console.WriteLine("Aspect code.");
      return typeof(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug582_EqualityContract.Target);
    }
  }
}