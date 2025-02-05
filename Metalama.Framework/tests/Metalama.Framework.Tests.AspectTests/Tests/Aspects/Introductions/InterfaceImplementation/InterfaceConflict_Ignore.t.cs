[Introduction]
public class TargetClass : IInterface
{
  int IInterface.InterfaceMethod()
  {
    Console.WriteLine("This is the original implementation.");
    return 42;
  }
}