// Final Compilation.Emit failed.
// Warning LAMA0282 on `Target`: `The aspect 'TheAspect' uses features of C# 11.0, but it is used in a project built with C# 10.0. Consider specifying <LangVersion>11.0</LangVersion> in this project or removing newer language features from the template 'TheAspect.Property1.get' and then specifying <MetalamaTemplateLanguageVersion> in the aspect project.`
// Error CS8936 on `"""get"""`: `Feature 'raw string literals' is not available in C# 10.0. Please use language version 11.0 or greater.`
// Error CS8936 on `"""set"""`: `Feature 'raw string literals' is not available in C# 10.0. Please use language version 11.0 or greater.`
[TheAspect]
class Target
{
  public global::System.String Property1
  {
    get
    {
      return (global::System.String)"""get""";
    }
  }
  public global::System.String Property2
  {
    get
    {
      return (global::System.String)"";
    }
    set
    {
      global::System.Console.WriteLine("""set""");
    }
  }
}