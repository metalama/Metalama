internal class Base
{
  protected void Add_Interceptor(global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Instance_BaseType.Bag receiver, global::System.Int32 value)
  {
    receiver.Add(value);
    return;
  }
}
[Redirect]
internal class Derived : Base
{
  private readonly Bag _bag = new();
  public void Fill() => this.Add_Interceptor(this._bag, 1);
}