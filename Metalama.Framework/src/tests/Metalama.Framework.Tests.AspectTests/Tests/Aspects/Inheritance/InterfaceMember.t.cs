internal sealed class SomeImplementation : IInterfaceB
{
  public Task SomeMethodAsync()
  {
    global::System.Console.WriteLine("overridden");
    return Task.CompletedTask;
  }
}