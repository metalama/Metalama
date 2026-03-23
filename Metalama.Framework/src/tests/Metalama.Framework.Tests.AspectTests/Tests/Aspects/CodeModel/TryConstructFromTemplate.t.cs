internal class Target
{
  [TestAspect]
  [MyCustomAttribute("Hello")]
  private void M()
  {
    global::System.Console.WriteLine("TryConstruct<T>: Hello");
    global::System.Console.WriteLine("TryConstruct: Hello");
    global::System.Console.WriteLine("Construct<T>: Hello");
    global::System.Console.WriteLine("Construct: Hello");
    return;
  }
}