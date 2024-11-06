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
    global::System.Console.WriteLine("Member: IInterface.Method(), Action: Introduce, Target: TargetClass.IInterface.Method()");
    global::System.Console.WriteLine("Member: Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.IInterface.Property, Action: Introduce, Target: TargetClass.IInterface.Property");
    global::System.Console.WriteLine("Member: IInterface.Event, Action: Introduce, Target: TargetClass.IInterface.Event");
    global::System.Console.WriteLine("Member: IBaseInterface.BaseMethod(), Action: Introduce, Target: TargetClass.IBaseInterface.BaseMethod()");
    global::System.Console.WriteLine("Member: Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.IBaseInterface.BaseProperty, Action: Introduce, Target: TargetClass.IBaseInterface.BaseProperty");
    global::System.Console.WriteLine("Member: IBaseInterface.BaseEvent, Action: Introduce, Target: TargetClass.IBaseInterface.BaseEvent");
  }
  global::System.Int32 global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.IBaseInterface.BaseProperty { get; set; }
  global::System.Int32 global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.IInterface.Property { get; set; }
  void global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.IBaseInterface.BaseMethod()
  {
  }
  void global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.IInterface.Method()
  {
  }
  private event global::System.EventHandler? _baseEvent;
  event global::System.EventHandler? global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.IBaseInterface.BaseEvent
  {
    add
    {
      this._baseEvent += value;
    }
    remove
    {
      this._baseEvent -= value;
    }
  }
  private event global::System.EventHandler? _event;
  event global::System.EventHandler? global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.IInterface.Event
  {
    add
    {
      this._event += value;
    }
    remove
    {
      this._event -= value;
    }
  }
}