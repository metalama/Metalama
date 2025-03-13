[TheAspect]
internal class Target
{
  public global::System.String Property
  {
    get
    {
      return (global::System.String)"""
           This is a long message.
           It has several lines.
               Some are indented
                       more than others.
           Some should start at the first column.
           Some have "quoted text" in them.
           """;
    }
  }
}