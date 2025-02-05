class Target : Base
{
  public override int Foo()
  {
    Console.WriteLine("Before");
    base.Foo();
    Console.WriteLine("After");
    return 42;
  }
}