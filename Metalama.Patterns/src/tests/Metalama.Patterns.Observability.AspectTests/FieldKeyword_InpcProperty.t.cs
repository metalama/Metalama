[Observable]
public class FieldKeyword_InpcProperty : INotifyPropertyChanged
{
  private SimpleInpcByHand _child = default !;
  // Semi-automatic property with INPC type.
  public SimpleInpcByHand Child
  {
    get
    {
      return _child;
    }
    set
    {
      if (!object.ReferenceEquals(value, _child))
      {
        var oldValue = _child;
        if (oldValue != null)
        {
          oldValue.PropertyChanged -= _handleChildPropertyChanged;
        }
        _child = value;
        OnObservablePropertyChanged("Child", oldValue, (INotifyPropertyChanged? )value);
        OnPropertyChanged("ChildValue");
        OnPropertyChanged("Child");
        SubscribeToChild(value);
      }
    }
  }
  private SimpleInpcByHand Child_Source { get => field; set => field = value ?? field; }
  // Computed property accessing the child's property.
  public int? ChildValue => this.Child?.A;
  private PropertyChangedEventHandler? _handleChildPropertyChanged;
  [ObservedExpressions("Child")]
  protected virtual void OnChildPropertyChanged(string parentPropertyPath, string propertyName)
  {
  }
  [ObservedExpressions("Child")]
  protected virtual void OnObservablePropertyChanged(string propertyPath, INotifyPropertyChanged? oldValue, INotifyPropertyChanged? newValue)
  {
  }
  protected virtual void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  private void SubscribeToChild(SimpleInpcByHand value)
  {
    if (value != null)
    {
      _handleChildPropertyChanged ??= HandlePropertyChanged;
      value.PropertyChanged += _handleChildPropertyChanged;
    }
    void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
      {
        var propertyName = e.PropertyName;
        switch (propertyName)
        {
          case "A":
            OnPropertyChanged("ChildValue");
            OnChildPropertyChanged("Child", "A");
            break;
          default:
            OnChildPropertyChanged("Child", propertyName);
            break;
        }
      }
    }
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}
