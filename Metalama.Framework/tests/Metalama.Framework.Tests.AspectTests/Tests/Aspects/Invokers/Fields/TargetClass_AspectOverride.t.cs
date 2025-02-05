public class TargetClass
{
  private global::System.Int32 _field;
  [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.TargetClass_AspectOverride.OverrideAspect]
  public global::System.Int32 Field
  {
    get
    {
      // Invoke this._field
      _ = this._field;
      // Invoke this._field
      _ = this._field;
      // Invoke this.Field
      _ = this.Field;
      // Invoke this.Field
      _ = this.Field;
      // Invoke this._field
      return this._field;
    }
    set
    {
      // Invoke this._field
      this._field = 42;
      // Invoke this._field
      this._field = 42;
      // Invoke this.Field
      this.Field = 42;
      // Invoke this.Field
      this.Field = 42;
      // Invoke this._field
      this._field = value;
    }
  }
  [InvokerBeforeAspect]
  public int InvokerBefore
  {
    get
    {
      // Invoke this.Field
      _ = this.Field;
      // Invoke this._field
      _ = this._field;
      // Invoke this._field
      _ = this._field;
      // Invoke this.Field
      _ = this.Field;
      // Invoke this._field
      return 0;
    }
    set
    { // Invoke this.Field
      this.Field = 42;
      // Invoke this._field
      this._field = 42;
      // Invoke this._field
      this._field = 42;
      // Invoke this.Field
      this.Field = 42;
    // Invoke this._field
    }
  }
  [InvokerAfterAspect]
  public int InvokerAfter
  {
    get
    {
      // Invoke this.Field
      _ = this.Field;
      // Invoke this.Field
      _ = this.Field;
      // Invoke this.Field
      _ = this.Field;
      // Invoke this.Field
      _ = this.Field;
      // Invoke this.Field
      return 0;
    }
    set
    { // Invoke this.Field
      this.Field = 42;
      // Invoke this.Field
      this.Field = 42;
      // Invoke this.Field
      this.Field = 42;
      // Invoke this.Field
      this.Field = 42;
    }
  }
}