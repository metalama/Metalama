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
    public T Value { get; set; }
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
    public string Tag { get; set; }
    public new TaggedKeyValue Build()
    {
      return new TaggedKeyValue(Tag, Value);
    }
  }
}