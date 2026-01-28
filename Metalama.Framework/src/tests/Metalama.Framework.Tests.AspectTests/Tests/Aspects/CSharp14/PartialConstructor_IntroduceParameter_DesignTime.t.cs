[TheAspect]
internal partial class ClassWithPartialConstructor
{
  public partial ClassWithPartialConstructor(int x);
  public partial ClassWithPartialConstructor(int x)
  {
    Console.WriteLine($"x={x}");
  }
}
