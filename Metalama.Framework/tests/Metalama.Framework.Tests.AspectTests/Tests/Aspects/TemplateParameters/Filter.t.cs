internal class Target
{
  [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplateParameters.Filter.MyAspect]
  private global::System.String? _q1;
  private global::System.String? q
  {
    get
    {
      return this._q1;
    }
    set
    {
      global::System.Console.WriteLine("hey");
      this._q1 = value;
    }
  }
}