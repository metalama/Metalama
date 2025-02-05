using Metalama.Framework.Code;
using Metalama.Framework.Options;

namespace Doc.AspectConfiguration
{
    // Options for the [Log] aspects.
    public class LoggingOptions : IHierarchicalOptions<IMethod>, IHierarchicalOptions<INamedType>,
                                  IHierarchicalOptions<INamespace>, IHierarchicalOptions<ICompilation>
    {
        public string? Category { get; init; }

        object IIncrementalObject.ApplyChanges( object changes, in ApplyChangesContext context )
        {
            var other = (LoggingOptions)changes;

            return new LoggingOptions { Category = other.Category ?? Category };
        }

        IHierarchicalOptions? IHierarchicalOptions.GetDefaultOptions( OptionsInitializationContext context ) => null;
    }
}