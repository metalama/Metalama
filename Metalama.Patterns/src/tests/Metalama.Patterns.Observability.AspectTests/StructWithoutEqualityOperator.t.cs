[Observable]
public class C : INotifyPropertyChanged
{
  private StructWithoutOperator _withoutOperator;
  public StructWithoutOperator WithoutOperator
  {
    get
    {
      return _withoutOperator;
    }
    set
    {
      if (!EqualityComparer<StructWithoutOperator>.Default.Equals(value, _withoutOperator))
      {
        _withoutOperator = value;
        OnPropertyChanged("WithoutOperator");
      }
    }
  }
  private StructWithoutOperator? _nullableWithoutOperator;
  public StructWithoutOperator? NullableWithoutOperator
  {
    get
    {
      return _nullableWithoutOperator;
    }
    set
    {
      if (!EqualityComparer<StructWithoutOperator?>.Default.Equals(value, _nullableWithoutOperator))
      {
        _nullableWithoutOperator = value;
        OnPropertyChanged("NullableWithoutOperator");
      }
    }
  }
  private StructWithOperator _withOperator;
  public StructWithOperator WithOperator
  {
    get
    {
      return _withOperator;
    }
    set
    {
      if (_withOperator != value)
      {
        _withOperator = value;
        OnPropertyChanged("WithOperator");
      }
    }
  }
  private StructWithOperator? _nullableWithOperator;
  public StructWithOperator? NullableWithOperator
  {
    get
    {
      return _nullableWithOperator;
    }
    set
    {
      if (_nullableWithOperator != value)
      {
        _nullableWithOperator = value;
        OnPropertyChanged("NullableWithOperator");
      }
    }
  }
  private RecordStruct _record;
  public RecordStruct Record
  {
    get
    {
      return _record;
    }
    set
    {
      if (_record != value)
      {
        _record = value;
        OnPropertyChanged("Record");
      }
    }
  }
  private MyEnum _enum;
  public MyEnum Enum
  {
    get
    {
      return _enum;
    }
    set
    {
      if (_enum != value)
      {
        _enum = value;
        OnPropertyChanged("Enum");
      }
    }
  }
  private int _int;
  public int Int
  {
    get
    {
      return _int;
    }
    set
    {
      if (_int != value)
      {
        _int = value;
        OnPropertyChanged("Int");
      }
    }
  }
  private int? _nullableInt;
  public int? NullableInt
  {
    get
    {
      return _nullableInt;
    }
    set
    {
      if (_nullableInt != value)
      {
        _nullableInt = value;
        OnPropertyChanged("NullableInt");
      }
    }
  }
  private DateTime _dateTime;
  public DateTime DateTime
  {
    get
    {
      return _dateTime;
    }
    set
    {
      if (_dateTime != value)
      {
        _dateTime = value;
        OnPropertyChanged("DateTime");
      }
    }
  }
  protected virtual void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}