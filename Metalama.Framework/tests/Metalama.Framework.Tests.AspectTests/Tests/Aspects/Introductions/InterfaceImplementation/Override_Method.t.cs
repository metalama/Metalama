[TheAspect]
internal class C : IDisposable
{
  public void Dispose()
  {
    Console.WriteLine("C.Dispose()");
    global::System.Console.WriteLine("TheAspect.Dispose()");
  }
}