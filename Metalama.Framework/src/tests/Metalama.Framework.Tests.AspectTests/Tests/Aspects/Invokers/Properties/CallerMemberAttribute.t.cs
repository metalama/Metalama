    [InvokerAspect]
public class TargetClass
{
  public int TestProperty
  {
    get
    {
      return 0;
    }
    set
    {
      OtherClass.Foo();
    }
  }
  [OverrideAspect]
  public int TestOverriddenProperty
  {
    get
    {
      return 0;
    }
    set
    {
      OtherClass.Foo();
    }
  }
  [OverrideAspect]
  public int TestOverriddenNonInlinedProperty
  {
    get
    {
      return this.TestOverriddenNonInlinedProperty_Source;
    }
    set
    {
      this.TestOverriddenNonInlinedProperty_Source = value;
      this.TestOverriddenNonInlinedProperty_Source = value;
    }
  }
  private int TestOverriddenNonInlinedProperty_Source
  {
    get
    {
      return 0;
    }
    set
    {
      OtherClass.Foo(callerMemberName: "TestOverriddenNonInlinedProperty");
    }
  }
  public void CallFoo()
  {
    this.TestProperty = 42;
    this.TestProperty = 42;
    this.TestProperty = 42;
    this.TestProperty = 42;
    this.TestOverriddenProperty = 42;
    this.TestOverriddenProperty = 42;
    this.TestOverriddenProperty = 42;
    this.TestOverriddenProperty = 42;
    this.TestOverriddenNonInlinedProperty = 42;
    this.TestOverriddenNonInlinedProperty = 42;
    this.TestOverriddenNonInlinedProperty = 42;
    this.TestOverriddenNonInlinedProperty = 42;
  }
}