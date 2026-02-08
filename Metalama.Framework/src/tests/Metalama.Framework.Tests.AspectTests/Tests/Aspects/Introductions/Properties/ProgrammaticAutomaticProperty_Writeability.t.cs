[MyAspect]
public class C
{
  private global::System.Int32 _getOnlyProp;
  private global::System.Int32 GetOnlyProp
  {
    get
    {
      return this._getOnlyProp;
    }
  }
  private global::System.Int32 _initOnlyProp;
  private global::System.Int32 InitOnlyProp
  {
    get
    {
      return this._initOnlyProp;
    }
    init
    {
      this._initOnlyProp = value;
    }
  }
  private global::System.Int32 _readWriteProp;
  private global::System.Int32 ReadWriteProp
  {
    get
    {
      return this._readWriteProp;
    }
    set
    {
      this._readWriteProp = value;
    }
  }
}
