namespace BlazorApp;

/// <summary>
/// A run-time class with a method to which the <see cref="Log1741Attribute"/> aspect is applied, so the project
/// contains a Metalama transformation that must run during the Razor declaration pass.
/// </summary>
public class Greeter1741
{
    [Log1741]
    public string Greet( string name ) => $"Hello, {name}!";
}
