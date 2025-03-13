internal class TargetClass
{
  private global::System.Object _field = default !;
  [global::Metalama.Framework.Tests.AspectTests.Aspects.Bugs.Bug29235.Aspect]
  public global::System.Object Field
  {
    get
    {
      global::System.Console.WriteLine("Overridden getter.");
      return this._field;
    }
    set
    {
      global::System.Console.WriteLine("Overridden setter.");
      this._field = value;
    }
  }
  private readonly object _property = default !;
  [Aspect]
  public object Property
  {
    get
    {
      global::System.Console.WriteLine("Overridden getter.");
      return this._property;
    }
    private init
    {
      global::System.Console.WriteLine("Overridden setter.");
      this._property = value;
    }
  }
  public TargetClass(object value)
  {
    Field = value;
    Property = value;
  }
}