// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.AspectOrdering;

internal sealed class AttributeAspectOrderingSource : IAspectOrderingSource
{
    private readonly Compilation _compilation;
    private readonly IAttributeDeserializer _attributeDeserializer;

    public AttributeAspectOrderingSource( in ProjectServiceProvider serviceProvider, CompilationContext compilationContext )
    {
        this._compilation = compilationContext.Compilation;
        this._attributeDeserializer = serviceProvider.GetRequiredService<SystemAttributeDeserializer.Provider>().Get( compilationContext );
    }

    public IEnumerable<AspectOrderSpecification> GetAspectOrderSpecification( IDiagnosticAdder diagnosticAdder )
    {
        var roslynCompilation = this._compilation;

        // Get compile-time level attributes of the current assembly and all referenced assemblies.
        var orderAttributeName = typeof(AspectOrderAttribute).FullName;

        var attributes =
            roslynCompilation.Assembly.Modules
                .SelectMany( m => m.ReferencedAssemblySymbols )
                .Concat( [roslynCompilation.Assembly] )
                .SelectMany( assembly => assembly.GetAttributes().Select( attribute => (attribute, assembly) ) )
                .Where( a => a.attribute.AttributeClass?.GetReflectionFullName() == orderAttributeName );

        return attributes.Select(
                attribute =>
                {
                    if ( this._attributeDeserializer.TryCreateAttribute<AspectOrderAttribute>(
                            attribute.attribute,
                            diagnosticAdder,
                            out var attributeInstance ) )
                    {
                        return new AspectOrderSpecification( attributeInstance, attribute.attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() );
                    }
                    else
                    {
                        return null;
                    }
                } )
            .WhereNotNull();
    }
}