partial class Target
{
  [TheAspect]
  partial string this[int i] { get; set; }
  partial string this[int i]
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
  partial string this[string s] { get; set; }
  [TheAspect]
  partial string this[string s]
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
  partial string this[long i] { get; }
  partial string this[long i]
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