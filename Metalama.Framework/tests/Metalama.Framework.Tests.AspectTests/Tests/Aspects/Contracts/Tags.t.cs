internal class Target
{
  [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Tags.MyAspect]
  private global::System.String? _q1;
  private global::System.String? q
  {
    get
    {
      return this._q1;
    }
    set
    {
      global::System.Console.WriteLine("tag");
      this._q1 = value;
    }
  }
}