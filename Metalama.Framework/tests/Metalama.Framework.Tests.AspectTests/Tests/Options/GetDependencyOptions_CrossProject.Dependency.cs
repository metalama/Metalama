using Metalama.Framework.Code;
using Metalama.Framework.Options;

namespace Metalama.Framework.Tests.AspectTests.Tests.Options.GetDependencyOptions_CrossProject;

public class C { }

public record Options : IHierarchicalOptions<IDeclaration>
{
    public string? ProjectPath { get; init; }

    public IHierarchicalOptions GetDefaultOptions(OptionsInitializationContext context)
        => new Options { ProjectPath = context.Project.PreprocessorSymbols.Contains("DEPENDENCY") ? "Dependency" : context.Project.Name };

    public object ApplyChanges(object changes, in ApplyChangesContext context)
    {
        var other = (Options)changes;

        return new Options { ProjectPath = $"{this.ProjectPath}->{other.ProjectPath}" };
    }
}