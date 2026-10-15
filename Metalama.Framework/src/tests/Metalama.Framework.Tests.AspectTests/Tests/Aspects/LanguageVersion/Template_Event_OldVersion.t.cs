// Final Compilation.Emit failed.
// Warning LAMA0282 on `Target`: `The aspect 'TheAspect' uses features of C# 11.0, but it is used in a project built with C# 10.0. Consider specifying <LangVersion>11.0</LangVersion> in this project or removing newer language features from the template 'TheAspect.Event1.add' and then specifying <MetalamaTemplateLanguageVersion> in the aspect project.`
// Error CS8936 on `"""add"""`: `Feature 'raw string literals' is not available in C# 10.0. Please use language version 11.0 or greater.`
// Error LAMA0232 on `"""add"""`: `Template code must be compatible with C# 10.0, but this syntax requires a later version of C#. The version is C# 10.0 because the compile-time code of the project is compiled with C# 10.0, which is the LangVersion of the project capped by the version that the .NET SDK supports. Set the MetalamaTemplateLanguageVersion property to choose the version explicitly.`
// Error CS8936 on `"""remove"""`: `Feature 'raw string literals' is not available in C# 10.0. Please use language version 11.0 or greater.`
// Error LAMA0232 on `"""remove"""`: `Template code must be compatible with C# 10.0, but this syntax requires a later version of C#. The version is C# 10.0 because the compile-time code of the project is compiled with C# 10.0, which is the LangVersion of the project capped by the version that the .NET SDK supports. Set the MetalamaTemplateLanguageVersion property to choose the version explicitly.`
[TheAspect]
internal class Target
{
  public event global::System.EventHandler Event1
  {
    add
    {
      global::System.Console.WriteLine("""add""");
    }
    remove
    {
    }
  }
  public event global::System.EventHandler Event2
  {
    add
    {
    }
    remove
    {
      global::System.Console.WriteLine("""remove""");
    }
  }
}