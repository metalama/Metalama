internal class BaseClass
{
  ~BaseClass()
  {
    Console.WriteLine($"This is the original finalizer.");
  }
}
[Override]
internal class DerivedClass : BaseClass
{
  ~DerivedClass()
  {
    global::System.Console.WriteLine("This is the override.");
    global::System.Console.WriteLine("This is the introduction.");
    return;
  }
}