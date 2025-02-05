[Introduction]
internal class TargetClass : DerivedClass
{
  public new event global::System.EventHandler? BaseClassAbstractEvent;
  public new event global::System.EventHandler? BaseClassAbstractSealedEvent;
  public new event global::System.EventHandler? BaseClassEvent;
  public static new event global::System.EventHandler? BaseClassEvent_Static;
  public new event global::System.EventHandler? BaseClassVirtualEvent;
  public new event global::System.EventHandler? BaseClassVirtualOverridenEvent;
  public new event global::System.EventHandler? BaseClassVirtualSealedEvent;
  public new event global::System.EventHandler? DerivedClassEvent;
  public static new event global::System.EventHandler? DerivedClassEvent_Static;
  public new event global::System.EventHandler? DerivedClassVirtualEvent;
  public new event global::System.EventHandler? DerivedClassVirtualSealedEvent;
  public new event global::System.EventHandler? HiddenBaseClassEvent;
  public static new event global::System.EventHandler? HiddenBaseClassEvent_Static;
  public new event global::System.EventHandler? HiddenBaseClassVirtualEvent;
  public new event global::System.EventHandler? HiddenVirtualBaseClassVirtualEvent;
  public event global::System.EventHandler? NonExistentEvent;
  public static event global::System.EventHandler? NonExistentEvent_Static;
}