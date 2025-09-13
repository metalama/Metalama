[Aspect]
internal class C
{
  private void M()
  {
    Log("foo");
    static void Log(string instance) => global::System.Console.WriteLine(instance);
  }
}