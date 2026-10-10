[TheAspect]
internal abstract class B : A
{
  public override void Dispose()
  {
    global::System.Console.WriteLine("TheAspect.Dispose()");
  }
}