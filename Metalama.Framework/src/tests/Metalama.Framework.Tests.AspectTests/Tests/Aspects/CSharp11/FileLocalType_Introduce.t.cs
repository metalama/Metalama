[IntroduceMembers]
file class FileLocalTarget
{
  public int Existing { get; set; }
  public global::System.Int32 IntroducedProperty { get; set; }
  public global::System.Int32 IntroducedMethod(global::System.Int32 a)
  {
    return a;
  }
  public event global::System.EventHandler? IntroducedEvent;
}