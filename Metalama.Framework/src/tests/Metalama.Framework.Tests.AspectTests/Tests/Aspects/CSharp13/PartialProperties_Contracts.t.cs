internal partial class Target
{
  [TheAspect]
  private partial string P1 { get; set; }
  private partial string P1
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
  private partial string P2 { get; set; }
  [TheAspect]
  private partial string P2
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
  private partial string P3 { get; }
  private partial string P3
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