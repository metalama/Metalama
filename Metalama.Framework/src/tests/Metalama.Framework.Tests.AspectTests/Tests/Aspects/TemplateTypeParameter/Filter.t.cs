internal class Target
{
  [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplateTypeParameters.Filter.MyAspect]
  private global::System.String? _q1;
  private global::System.String? q
  {
    get
    {
      return this._q1;
    }
    set
    {
      global::System.Console.WriteLine(typeof(global::System.String).Name);
      this._q1 = value;
    }
  }
}