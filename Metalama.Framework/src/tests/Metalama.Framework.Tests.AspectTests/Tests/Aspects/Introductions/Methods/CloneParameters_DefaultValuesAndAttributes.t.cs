[Copy]
public class Target
{
  public void Source([Description("The count.")] int count = 5, string? name = null, params string[] rest)
  {
  }
  public void CopyWithAll([global::System.ComponentModel.DescriptionAttribute("The count.")] global::System.Int32 count = 5, global::System.String? name = null, params global::System.String[] rest)
  {
  }
  public void CopyWithNone(global::System.Int32 count, global::System.String? name, global::System.String[] rest)
  {
  }
}