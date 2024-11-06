[Introduction]
public class TargetClass : global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.IInterface, global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.IBaseInterface
{
  public void BaseMethod()
  {
  }
  public int BaseProperty { get; set; }
  public event EventHandler? BaseEvent;
  public void Method()
  {
  }
  public int Property { get; set; }
  public event EventHandler? Event;
  public void Witness()
  {
    global::System.Console.WriteLine("InterfaceType: IInterface, Action: Implement");
    global::System.Console.WriteLine("InterfaceType: IBaseInterface, Action: Implement");
    global::System.Console.WriteLine("Member: IInterface.Method(), Action: UseExisting, Target: TargetClass.Method()");
    global::System.Console.WriteLine("Member: Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.IInterface.Property, Action: UseExisting, Target: Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.AdviceResult_Fail_MembersIgnore.TargetClass.Property");
    global::System.Console.WriteLine("Member: IInterface.Event, Action: UseExisting, Target: TargetClass.Event");
    global::System.Console.WriteLine("Member: IBaseInterface.BaseMethod(), Action: UseExisting, Target: TargetClass.BaseMethod()");
    global::System.Console.WriteLine("Member: Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.IBaseInterface.BaseProperty, Action: UseExisting, Target: Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.AdviceResult_Fail_MembersIgnore.TargetClass.BaseProperty");
    global::System.Console.WriteLine("Member: IBaseInterface.BaseEvent, Action: UseExisting, Target: TargetClass.BaseEvent");
  }
}