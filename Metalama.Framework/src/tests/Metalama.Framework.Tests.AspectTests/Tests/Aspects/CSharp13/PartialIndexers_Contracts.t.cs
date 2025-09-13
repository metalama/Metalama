internal partial class Target
{
  [TheAspect]
  private partial string this[int i] { get; set; }
  private partial string this[int i]
  {
    get => "foo";
    set
    {
      if (value == null)
      {
        throw new global::System.ArgumentNullException("Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp13.PartialIndexers_Contracts.Target.this[int]");
      }
      throw new Exception();
    }
  }
  private partial string this[string s] { get; set; }
  [TheAspect]
  private partial string this[string s]
  {
    get => "foo";
    set
    {
      if (value == null)
      {
        throw new global::System.ArgumentNullException("Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp13.PartialIndexers_Contracts.Target.this[string]");
      }
      throw new Exception();
    }
  }
  [TheAspect]
  private partial string this[long i] { get; }
  private partial string this[long i]
  {
    get
    {
      global::System.String returnValue;
      returnValue = "foo";
      if (returnValue == null)
      {
        throw new global::System.ArgumentNullException("Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp13.PartialIndexers_Contracts.Target.this[long]");
      }
      return returnValue;
    }
  }
}