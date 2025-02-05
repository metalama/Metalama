[MyInheritableAspectWhichIntroducesAMethod]
internal class Foo<T>
{
  public void M()
  {
  }
}
internal class Bar : Foo<int>
{
  private void CallM()
  {
    this.M();
  }
}