using System.Collections.Immutable;
using Metalama.Reactive.Sources;

namespace Metalama.Reactive.UnitTests
{

    internal class SourceType
    {
        public SourceType( string name, IImmutableList<string>? baseTypes )
        {
            this.Name = name;
            this.BaseTypes = baseTypes ?? ImmutableList<string>.Empty;
        }

        public string Name { get; }

        public IImmutableList<string> BaseTypes { get; }

        public ReactiveHashSet<Member> Members { get; } = new();

        public ReactiveHashSet<SourceType> NestedTypes { get; } = new();
    }

    internal record Member( string Name );
}
