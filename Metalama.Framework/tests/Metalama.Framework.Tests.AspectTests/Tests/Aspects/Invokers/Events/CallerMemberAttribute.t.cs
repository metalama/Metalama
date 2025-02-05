[InvokerAspect]
public class TargetClass
{
  public event EventHandler TestEvent
  {
    add
    {
    }
    remove
    {
      OtherClass.Foo();
    }
  }
  [OverrideAspect]
  public event EventHandler TestOverriddenEvent
  {
    add
    {
    }
    remove
    {
      OtherClass.Foo();
    }
  }
  [OverrideAspect]
  public event EventHandler TestOverriddenNonInlinedEvent
  {
    add
    {
      this.TestOverriddenNonInlinedEvent_Source += value;
      this.TestOverriddenNonInlinedEvent_Source += value;
    }
    remove
    {
      this.TestOverriddenNonInlinedEvent_Source -= value;
    }
  }
  private event EventHandler TestOverriddenNonInlinedEvent_Source
  {
    add
    {
    }
    remove
    {
      OtherClass.Foo(callerMemberName: "TestOverriddenNonInlinedEvent");
    }
  }
  public void CallFoo()
  {
    this.TestEvent += null;
    this.TestEvent += null;
    this.TestEvent += null;
    this.TestEvent += null;
    this.TestOverriddenEvent += null;
    this.TestOverriddenEvent += null;
    this.TestOverriddenEvent += null;
    this.TestOverriddenEvent += null;
    this.TestOverriddenNonInlinedEvent += null;
    this.TestOverriddenNonInlinedEvent += null;
    this.TestOverriddenNonInlinedEvent += null;
    this.TestOverriddenNonInlinedEvent += null;
  }
}