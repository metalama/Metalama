[GenerateBuilder]
public class StringKeyedValue<T>
{
  public T Value { get; }
  protected StringKeyedValue(T value)
  {
    Value = value;
  }
  public virtual Builder ToBuilder()
  {
    return new StringKeyedValue<T>.Builder(this);
  }
  public class Builder
  {
    public Builder()
    {
    }
    protected internal Builder(StringKeyedValue<T> source)
    {
      Value = source.Value;
    }
    private T _value = default !;
    public T Value
    {
      get
      {
        return _value;
      }
      set
      {
        _value = value;
      }
    }
    public StringKeyedValue<T> Build()
    {
      return new StringKeyedValue<T>(Value);
    }
  }
}
public class TaggedKeyValue : StringKeyedValue<string>
{
  public string Tag { get; }
  protected TaggedKeyValue(string tag, string value) : base(value)
  {
    Tag = tag;
  }
  public override Builder ToBuilder()
  {
    return new Builder(this);
  }
  public new class Builder : StringKeyedValue<string>.Builder
  {
    public Builder() : base()
    {
    }
    protected internal Builder(TaggedKeyValue source) : base(source)
    {
      Tag = source.Tag;
    }
    private string _tag = default !;
    public string Tag
    {
      get
      {
        return _tag;
      }
      set
      {
        _tag = value;
      }
    }
    public new TaggedKeyValue Build()
    {
      return new TaggedKeyValue(Tag, Value);
    }
  }
}