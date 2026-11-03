// Final Compilation.Emit failed.
// Warning LAMA0282 on `Target`: `The aspect 'TheAspect' uses features of C# 11.0, but it is used in a project built with C# 10.0. Consider specifying <LangVersion>11.0</LangVersion> in this project or removing newer language features from the template 'TheAspect.Property' and then specifying <MetalamaTemplateLanguageVersion> in the aspect project.`
// Error CS8936 on `""" This is a long message. It has several lines. Some are indented more than others. Some should start at the first column. Some have "quoted text" in them. """`: `Feature 'raw string literals' is not available in C# 10.0. Please use language version 11.0 or greater.`
// Error LAMA0232 on `""" This is a long message. It has several lines. Some are indented more than others. Some should start at the first column. Some have "quoted text" in them. """`: `Template code must be compatible with C# 10.0, but this syntax requires a later version of C#. The version is C# 10.0 because the compile-time code of the project is compiled with C# 10.0, which is the LangVersion of the project capped by the version that the compiler toolset used to build the project supports. Set the MetalamaTemplateLanguageVersion property to choose the version explicitly.`
[TheAspect]
internal class Target
{
  public global::System.String Property { get; } = (global::System.String)"""
                                      This is a long message.
                                      It has several lines.
                                          Some are indented
                                                  more than others.
                                      Some should start at the first column.
                                      Some have "quoted text" in them.
                                      """;
}