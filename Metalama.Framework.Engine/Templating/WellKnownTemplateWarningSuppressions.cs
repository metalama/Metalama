// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Diagnostics;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.Templating;

public static class WellKnownTemplateWarningSuppressions
{
    public static readonly IReadOnlyDictionary<string, SuppressionDefinition> SuppressionDescriptors =
        new[]
        {
            // CA1822 - Mark members as static    
            new SuppressionDefinition( "CA1822", "Member may use generate code that references non-static members." ),

            // IDE0031 - Use null propagation
            new SuppressionDefinition(
                "IDE0031",
                "Null propagation of compile-time variables to members returning a run-time value is not supported." ),

            // IDE0053 - Use expression body for lambdas
            new SuppressionDefinition( "IDE0053", "Lambda expressions returning a dynamic type are not supported in templates." ),

            // IDE0059 - Unnecessary assignment of a value
            new SuppressionDefinition(
                "IDE0059",
                "Parameters in a contract or initializer template are likely to be used by the overridden member." ),

            // CS0628 - New protected member declared in sealed type
            new SuppressionDefinition( "CS0628", "The target type of the aspect may be non-sealed." ),

            // CS8618: Non-nullable property 'property' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
            new SuppressionDefinition( "CS8618", "A template property may be initialized by the target code." ),

            // CS0169: Field is never used
            new SuppressionDefinition( "CS0169", "A template field may be used by the target code." )
        }.ToDictionary( d => d.SuppressedDiagnosticId, d => d );
}