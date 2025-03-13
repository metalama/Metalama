[Aspect]
internal class C
{
  private void M()
  {
    Log("foo");
    void Log(string instance) => global::System.Console.WriteLine(instance);
  }
}