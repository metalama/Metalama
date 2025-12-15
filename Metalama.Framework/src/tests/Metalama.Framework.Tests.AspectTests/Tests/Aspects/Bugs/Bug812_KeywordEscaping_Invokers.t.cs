internal class TargetClass
{
  public int @const;
  public string @int { get; set; }
  public void @void()
  {
  }
  public void MethodWithParam(string @class)
  {
  }
  public event Action @event;
  private int _fieldInvoker;
  [InvokeKeywordFieldAspect]
  public int FieldInvoker
  {
    get
    {
      _ = this.@const;
      _ = this.@const;
      return this._fieldInvoker;
    }
    set
    {
      this.@const = 42;
      this.@const = 42;
      this._fieldInvoker = value;
    }
  }
  private string _propertyInvoker = default !;
  [InvokeKeywordPropertyAspect]
  public string PropertyInvoker
  {
    get
    {
      _ = this.@int;
      return this._propertyInvoker;
    }
    set
    {
      this.@int = "test";
      this._propertyInvoker = value;
    }
  }
  [InvokeKeywordMethodAspect]
  public void MethodInvoker()
  {
    this.@void();
    return;
  }
}