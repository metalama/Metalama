[Introduce]
internal class Program : global::Metalama.Framework.IntegrationTests.Aspects.AspectMemberRef.InterfaceMemberRef.IInterface
{
  public global::System.Int32 Property { get; set; }
  private void EventHandler(global::System.Object? sender, global::System.EventArgs a)
  {
  }
  public void Method()
  {
  }
  public void SomeMethod()
  {
    Method();
    Property = Property + 1;
    Event += EventHandler;
  }
  public event global::System.EventHandler? Event;
}