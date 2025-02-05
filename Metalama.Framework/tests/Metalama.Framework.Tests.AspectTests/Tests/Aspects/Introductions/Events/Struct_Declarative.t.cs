[Introduction]
internal struct TargetStruct
{
  private int _existingField;
  public TargetStruct(int x)
  {
    _existingField = x;
  }
  public static void Foo(global::System.Object? sender, global::System.EventArgs args)
  {
  }
  public event global::System.EventHandler? IntroducedEvent;
  public event global::System.EventHandler? IntroducedEvent_Initializer = (global::System.EventHandler? )Foo;
  public event global::System.EventHandler? IntroducedEvent_Static;
  public event global::System.EventHandler? IntroducedEvent_Static_Initializer = (global::System.EventHandler? )Foo;
}