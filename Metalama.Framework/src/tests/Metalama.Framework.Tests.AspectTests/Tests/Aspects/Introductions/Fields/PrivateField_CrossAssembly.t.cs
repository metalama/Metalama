internal class Program
{
  [IntroducePrivateField]
  public void Foo()
  {
    global::System.Console.WriteLine(_text);
    return;
  }
  private readonly global::System.String _text = (global::System.String)"a text";
}