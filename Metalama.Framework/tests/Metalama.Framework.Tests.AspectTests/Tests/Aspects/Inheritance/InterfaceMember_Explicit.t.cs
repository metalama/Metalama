internal sealed class SomeImplementation : IInterfaceB
{
  Task IInterfaceA.SomeMethodAsync()
  {
    global::System.Console.WriteLine("overridden");
    return Task.CompletedTask;
  }
}