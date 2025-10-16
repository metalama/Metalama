[Parent]
internal partial class Foo : IGotParent
{
  object? IGotParent.Property
  {
    get
    {
      return null;
    }
  }
  event Action IGotParent.Event
  {
    add
    {
      global::System.Console.WriteLine("Adding");
    }
    remove
    {
      global::System.Console.WriteLine("Removing");
    }
  }
  int IGotParent.Method()
  {
    return (global::System.Int32)1;
  }
}