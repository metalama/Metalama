// Syntax tree verification failed.
// Error CS1031 on ``: `Type expected`
// Error CS1001 on `>`: `Identifier expected`
[Test]
internal partial class InvalidBase : SomethingThatDoesNotExist
{
  public void Foo()
  {
  }
}
[Test]
internal partial class MissingInterface : object, {
  public void Foo()
  {
  }
}
[Test]
internal partial class InvalidInterface : object, ISomethingThatDoesNotExist
{
  public void Foo()
  {
  }
}
[Test]
internal partial class InvalidTypeParameterList<T, >
{
  public void Foo()
  {
  }
}