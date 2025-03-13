[TrackChanges]
public partial class Comment
{
  public Guid Id { get; }
  public string Author { get; set; }
  public string Content { get; set; }
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
      }
    }
  }
}