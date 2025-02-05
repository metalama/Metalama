[IntroduceInterface]
[NotNull]
internal record Target : global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Property_IntroducedInterface.I
{
  global::System.String? global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Property_IntroducedInterface.I.M
  {
    get
    {
      global::System.String? returnValue;
      global::System.Console.WriteLine("Introduced.");
      returnValue = default(global::System.String? )!;
      if (returnValue == null)
      {
        throw new global::System.ArgumentNullException("Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Property_IntroducedInterface.I.M");
      }
      return returnValue;
    }
    set
    {
      if (value == null)
      {
        throw new global::System.ArgumentNullException("Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Property_IntroducedInterface.I.M");
      }
      global::System.Console.WriteLine("Introduced.");
    }
  }
  private global::System.String? _n = default;
  global::System.String? global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Property_IntroducedInterface.I.N
  {
    get
    {
      var returnValue = this._n;
      if (returnValue == null)
      {
        throw new global::System.ArgumentNullException("Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Property_IntroducedInterface.I.N");
      }
      return returnValue;
    }
    set
    {
      if (value == null)
      {
        throw new global::System.ArgumentNullException("Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Property_IntroducedInterface.I.N");
      }
      this._n = value;
    }
  }
}