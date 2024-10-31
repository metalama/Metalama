partial class Target
{
  [TheAspect]
  partial string P1 { get; set; }
  partial string P1
  {
    get => "foo";
    set
    {
      if (value == null)
      {
        throw new global::System.ArgumentNullException("Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp13.PartialProperties_Contracts.Target.P1");
      }
      throw new Exception();
    }
  }
  partial string P2 { get; set; }
  [TheAspect]
  partial string P2
  {
    get => "foo";
    set
    {
      if (value == null)
      {
        throw new global::System.ArgumentNullException("Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp13.PartialProperties_Contracts.Target.P2");
      }
      throw new Exception();
    }
  }
  [TheAspect]
  partial string P3 { get; }
  partial string P3
  {
    get
    {
      global::System.String returnValue;
      returnValue = "foo";
      if (returnValue == null)
      {
        throw new global::System.ArgumentNullException("Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp13.PartialProperties_Contracts.Target.P3");
      }
      return returnValue;
    }
  }
}