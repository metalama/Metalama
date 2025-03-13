[XpoDefaultValueAutoImplementation]
public sealed class MyXpObject : BaseXPObject
{
  public override void AfterConstruction()
  {
    global::System.Console.WriteLine("Overridden!");
    base.AfterConstruction();
  }
}