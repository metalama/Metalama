public interface I
{
  void M([DerivedAspect] int x);
}
public class C : I
{
  public void M([DerivedAspect] int x)
  {
    global::System.Console.WriteLine("Again");
  }
}