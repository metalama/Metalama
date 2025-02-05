internal class TargetCode
{
  private class Nullable
  {
    private global::System.String? _field = null;
    [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Nullable.DynamicParameterContract.Aspect]
    public global::System.String? Field
    {
      get
      {
        return this._field;
      }
      set
      {
        value?.ToString();
        value!.ToString();
        this._field = value;
      }
    }
    private string? _property;
    [Aspect]
    public string? Property
    {
      get
      {
        return this._property;
      }
      set
      {
        value?.ToString();
        value!.ToString();
        this._property = value;
      }
    }
    [Aspect]
    public string? this[int i]
    {
      get
      {
        global::System.String? returnValue;
        returnValue = null;
        returnValue?.ToString();
        returnValue!.ToString();
        return returnValue;
      }
    }
    [return: Aspect]
    private string? Method([Aspect] string? arg)
    {
      arg?.ToString();
      arg!.ToString();
      global::System.String? returnValue;
      returnValue = arg;
      returnValue?.ToString();
      returnValue!.ToString();
      return returnValue;
    }
  }
  private class NotNullable
  {
    private global::System.String _field = null !;
    [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Nullable.DynamicParameterContract.Aspect]
    public global::System.String Field
    {
      get
      {
        return this._field;
      }
      set
      {
        value.ToString();
        value.ToString();
        this._field = value;
      }
    }
    private string _property = null !;
    [Aspect]
    public string Property
    {
      get
      {
        return this._property;
      }
      set
      {
        value.ToString();
        value.ToString();
        this._property = value;
      }
    }
    [Aspect]
    public string this[int i]
    {
      get
      {
        global::System.String returnValue;
        returnValue = null !;
        returnValue.ToString();
        returnValue.ToString();
        return returnValue;
      }
    }
    [return: Aspect]
    private string Method([Aspect] string arg)
    {
      arg.ToString();
      arg.ToString();
      global::System.String returnValue;
      returnValue = arg;
      returnValue.ToString();
      returnValue.ToString();
      return returnValue;
    }
  }
#nullable disable
  private class Oblivious
  {
    private global::System.String _field = null;
    [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Nullable.DynamicParameterContract.Aspect]
    public global::System.String Field
    {
      get
      {
        return this._field;
      }
      set
      {
        value?.ToString();
        value.ToString();
        this._field = value;
      }
    }
    private string _property;
    [Aspect]
    public string Property
    {
      get
      {
        return this._property;
      }
      set
      {
        value?.ToString();
        value.ToString();
        this._property = value;
      }
    }
    [Aspect]
    public string this[int i]
    {
      get
      {
        global::System.String returnValue;
        returnValue = null;
        returnValue?.ToString();
        returnValue.ToString();
        return returnValue;
      }
    }
    [return: Aspect]
    private string Method([Aspect] string arg)
    {
      arg?.ToString();
      arg.ToString();
      global::System.String returnValue;
      returnValue = arg;
      returnValue?.ToString();
      returnValue.ToString();
      return returnValue;
    }
  }
}