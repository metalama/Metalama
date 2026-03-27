[Observable]
public sealed partial class SealedWithDeepChild : INotifyPropertyChanged
{
  private Inner _child = default !;
  public Inner Child
  {
    get
    {
      return _child;
    }
    set
    {
      // Template: OverrideInpcRefTypePropertySetter
      if (!object.ReferenceEquals(value, _child))
      {
        var oldValue = _child;
        if (oldValue != null)
        {
          oldValue.PropertyChanged -= _handleChildPropertyChanged;
        }
        _child = value;
        UpdateChildLeaf();
        OnPropertyChanged("Child");
        SubscribeToChild(value);
      }
    }
  }
  public int Derived => this.Child.Leaf.DeepValue;
  private PropertyChangedEventHandler? _handleChildLeafPropertyChanged;
  private PropertyChangedEventHandler? _handleChildPropertyChanged;
  private Leaf? _lastChildLeaf;
  private void OnPropertyChanged(string propertyName)
  { // Template: OnPropertyChanged
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  private void SubscribeToChild(Inner value)
  {
    if (value != null)
    {
      _handleChildPropertyChanged ??= HandlePropertyChanged;
      value.PropertyChanged += _handleChildPropertyChanged;
    }
    void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
      {
        // Template: HandleChildPropertyChangedDelegateBody
        var propertyName = e.PropertyName;
        switch (propertyName)
        {
          case "Leaf":
            UpdateChildLeaf();
            break;
        }
      }
    }
  }
  private void UpdateChildLeaf()
  {
    // Template: UpdateChildInpcProperty
    var newValue = Child?.Leaf;
    if (!object.ReferenceEquals(newValue, _lastChildLeaf))
    {
      if (!object.ReferenceEquals(_lastChildLeaf, null))
      {
        _lastChildLeaf.PropertyChanged -= _handleChildLeafPropertyChanged;
      }
      if (newValue != null)
      {
        _handleChildLeafPropertyChanged ??= HandleChildPropertyChanged;
        newValue.PropertyChanged += _handleChildLeafPropertyChanged;
        void HandleChildPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
          {
            // Template: HandleChildPropertyChangedDelegateBody
            var propertyName = e.PropertyName;
            switch (propertyName)
            {
              case "DeepValue":
                OnPropertyChanged("Derived");
                break;
            }
          }
        }
      }
      _lastChildLeaf = newValue;
      OnPropertyChanged("Derived");
    // Not calling OnChildPropertyChanged('Child','Leaf') because the type is sealed and OnChildPropertyChanged is not available.
    }
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}