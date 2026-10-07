// Warning TEST0003 on `Add`: `The redirection of 'new Bag().Add( 3 )' was refused with ArgumentException: The call site 'new Bag().Add( 3 )' cannot be redirected to the instance method 'Program.Add_Interceptor(Bag, int)', because the call site is not in an instance member.`
[Redirect]
internal class Program
{
  public string Name { get; } = "the program";
  private readonly Bag _bag = new();
  public void Fill()
  {
    this.Add_Interceptor(this._bag, 1);
    Action add = () => this.Add_Interceptor(this._bag, 2);
    add();
  }
  public static void TestMain()
  {
    new Program().Fill();
    new Bag().Add(3);
  }
  private void Add_Interceptor(global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Instance_CallingType_This.Bag receiver, global::System.Int32 value)
  {
    global::System.Console.WriteLine($"Adding to the bag of {this.Name}.");
    receiver.Add(value);
    return;
  }
}