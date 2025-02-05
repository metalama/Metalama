// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using MethodKind = Microsoft.CodeAnalysis.MethodKind;

namespace Metalama.Framework.Engine.CompileTime
{
    internal sealed record TemplateClassMember(
        string Name,
        string Key,
        TemplateClass TemplateClass,
        ITemplateInfo TemplateInfo,
        IAdviceAttribute? Attribute,
        SerializableDeclarationId DeclarationId,
        ImmutableArray<TemplateClassMemberParameter> Parameters,
        ImmutableArray<TemplateClassMemberParameter> TypeParameters,
        ImmutableDictionary<MethodKind, TemplateClassMember> Accessors )
    {
        public ImmutableArray<TemplateClassMemberParameter> RunTimeParameters { get; } = Parameters.Where( p => !p.IsCompileTime ).ToImmutableArray();

        public ImmutableArray<TemplateClassMemberParameter> RunTimeTypeParameters { get; } = TypeParameters.Where( p => !p.IsCompileTime ).ToImmutableArray();

        public ImmutableDictionary<string, TemplateClassMemberParameter> IndexedParameters { get; } =
            Parameters.Concat( TypeParameters ).ToImmutableDictionary( x => x.Name, x => x );

        // ImmutableArray doesn't implement value equality, so revert back to reference equality,
        // instead of the mixed equality that would be provided by the compiler
        public bool Equals( TemplateClassMember? other ) => ReferenceEquals( this, other );

        public override int GetHashCode() => RuntimeHelpers.GetHashCode( this );
    }
}