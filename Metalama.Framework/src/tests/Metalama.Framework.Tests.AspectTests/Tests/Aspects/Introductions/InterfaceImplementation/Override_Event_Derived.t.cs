[TheAspect]
internal class D : C
{
  public override event global::System.EventHandler? Event
  {
    add
    {
      global::System.Console.WriteLine("TheAspect.Event");
      base.Event += value;
    }
    remove
    {
      global::System.Console.WriteLine("TheAspect.Event");
      base.Event -= value;
    }
  }
}