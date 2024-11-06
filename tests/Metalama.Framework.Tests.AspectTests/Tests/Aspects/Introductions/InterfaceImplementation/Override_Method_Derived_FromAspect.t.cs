[TheAspect]
internal class C : global::System.IDisposable
{
  public virtual void Dispose()
  {
    global::System.Console.WriteLine("TheAspect.Dispose()");
  }
}
internal class D : C
{
  public override void Dispose()
  {
    base.Dispose();
    global::System.Console.WriteLine("TheAspect.Dispose()");
  }
}