[Introduction]
public class TargetClass : IBaseInterface, global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.IInterface
{
  public void BaseMethod()
  {
  }
  private int _baseProperty;
  public int BaseProperty
  {
    get
    {
      return this._baseProperty;
    }
    set
    {
      this._baseProperty = value;
    }
  }
  private event EventHandler? _baseEvent;
  public event EventHandler? BaseEvent
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
  public void Method()
  {
  }
  private int _property;
  public int Property
  {
    get
    {
      return this._property;
    }
    set
    {
      this._property = value;
    }
  }
  private event EventHandler? _event;
  public event EventHandler? Event
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
  public void Witness()
  {
    global::System.Console.WriteLine("InterfaceType: IInterface, Action: Implement");
    global::System.Console.WriteLine("InterfaceType: IBaseInterface, Action: Implement");
    global::System.Console.WriteLine("Member: IInterface.Method(), Action: Override, Target: TargetClass.Method()");
    global::System.Console.WriteLine("Member: Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.IInterface.Property, Action: Override, Target: Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.AdviceResult_Override_ImplementedBase.TargetClass.Property");
    global::System.Console.WriteLine("Member: IInterface.Event, Action: Override, Target: TargetClass.Event");
    global::System.Console.WriteLine("Member: IBaseInterface.BaseMethod(), Action: Override, Target: TargetClass.BaseMethod()");
    global::System.Console.WriteLine("Member: Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.IBaseInterface.BaseProperty, Action: Override, Target: Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.AdviceResult_Override_ImplementedBase.TargetClass.BaseProperty");
    global::System.Console.WriteLine("Member: IBaseInterface.BaseEvent, Action: Override, Target: TargetClass.BaseEvent");
  }
}