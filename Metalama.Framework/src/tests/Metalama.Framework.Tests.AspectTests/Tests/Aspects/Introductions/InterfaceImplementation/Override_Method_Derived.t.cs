[TheAspect]
internal class D : C
{
  public override void Dispose()
  {
    base.Dispose();
    global::System.Console.WriteLine("TheAspect.Dispose()");
  }
}