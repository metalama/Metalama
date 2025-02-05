[TheAspect]
private void M()
{
  global::System.Console.WriteLine("Oops");
  var longMessage = """
                          This is a long message.
                          It has several lines.
                              Some are indented
                                      more than others.
                          Some should start at the first column.
                          Some have "quoted text" in them.
                          """;
  Console.WriteLine(longMessage);
  return;
}