[TheAspect]
internal class C : B
{
  public override event global::System.EventHandler Event
  {
    add
    {
      global::System.Console.WriteLine("TheAspect.Event.add");
      base.Event += value;
    }
    remove
    {
      global::System.Console.WriteLine("TheAspect.Event.remove");
      base.Event -= value;
    }
  }
}