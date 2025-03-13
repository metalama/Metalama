[TrackChanges]
[NotifyPropertyChanged]
public partial class Comment : global::System.ComponentModel.INotifyPropertyChanged, global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug32975.IChangeTracking
{
  public Guid Id { get; }
  private string _author = default !;
  public string Author
  {
    get
    {
      return this._author;
    }
    set
    {
      if (value != this._author)
      {
        this._author = value;
        OnPropertyChanged("Author");
      }
      return;
    }
  }
  private string _content = default !;
  public string Content
  {
    get
    {
      return this._content;
    }
    set
    {
      if (value != this._content)
      {
        this._content = value;
        OnPropertyChanged("Content");
      }
      return;
    }
  }
  public Comment(Guid id, string author, string content)
  {
    Id = id;
    Author = author;
    Content = content;
  }
  public global::System.Boolean? HasChanges { get; protected set; }
  public global::System.Boolean IsTrackingChanges
  {
    get
    {
      return (global::System.Boolean)HasChanges.HasValue;
    }
    set
    {
      if (IsTrackingChanges != value)
      {
        HasChanges = value ? false : null;
        this.OnPropertyChanged("IsTrackingChanges");
      }
    }
  }
  protected void OnChange()
  {
    if (HasChanges == false)
    {
      HasChanges = true;
      this.OnPropertyChanged("HasChanges");
    }
  }
  protected void OnPropertyChanged(global::System.String name)
  {
    PropertyChanged?.Invoke(this, new global::System.ComponentModel.PropertyChangedEventArgs(name));
  }
  public void ResetChanges()
  {
    if (IsTrackingChanges)
    {
      HasChanges = false;
    }
  }
  public event global::System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
}