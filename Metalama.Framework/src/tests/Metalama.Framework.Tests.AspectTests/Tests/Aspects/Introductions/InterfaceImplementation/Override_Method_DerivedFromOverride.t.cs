[TheAspect]
internal class C : B
{
  public override void Dispose()
  {
    base.Dispose();
    global::System.Console.WriteLine("TheAspect.Dispose()");
  }
}