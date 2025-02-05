[TheAspect]
private void M()
{
  var longMessage = """
                          This is a long message.
                          It has several lines.
                              Some are indented
                                      more than others.
                          Some should start at the first column.
                          Some have "quoted text" in them.
                          """;
  global::System.Console.WriteLine(longMessage);
  return;
}