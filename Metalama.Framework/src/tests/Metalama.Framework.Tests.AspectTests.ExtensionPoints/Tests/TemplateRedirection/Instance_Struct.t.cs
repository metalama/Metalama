[Redirect]
internal struct Filler
{
  private readonly Bag _bag;
  public Filler(Bag bag)
  {
    this._bag = bag;
  }
  public void Fill() => this.Add_Interceptor(this._bag, 1);
  private void Add_Interceptor(global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Instance_Struct.Bag receiver, global::System.Int32 value)
  {
    receiver.Add(value);
    return;
  }
}