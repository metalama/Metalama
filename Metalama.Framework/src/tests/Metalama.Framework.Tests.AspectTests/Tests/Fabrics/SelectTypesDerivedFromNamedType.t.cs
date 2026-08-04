internal class TargetCode
{
  internal class DerivedFromPlain : PlainBaseClass
  {
    public void MethodOnPlain()
    {
      global::System.Console.WriteLine("overridden");
      return;
    }
  }
  internal class DerivedFromInt : BaseClass<int>
  {
    public void MethodOnInt()
    {
    }
  }
  internal class DerivedFromString : BaseClass<string>
  {
    public void MethodOnString()
    {
    }
  }
}