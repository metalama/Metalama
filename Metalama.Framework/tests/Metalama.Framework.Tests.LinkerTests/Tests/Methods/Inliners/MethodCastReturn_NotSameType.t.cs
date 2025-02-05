class Target : Base
{
  public override int Foo()
  {
    Console.WriteLine("Before");
    return (int)base.Foo();
  }
}