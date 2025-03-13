// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.Templating;

public static class WellKnownTemplateWarningSuppressions
{
    private static readonly SymbolKind[] _anyTemplate = [SymbolKind.Method, SymbolKind.Property, SymbolKind.Event, SymbolKind.Field];
    
    public static readonly IReadOnlyDictionary<string, WellKnownTemplateWarningSuppression> SuppressionDescriptors =
        new[]
        {
            // CA1822 - Mark members as static    
            new WellKnownTemplateWarningSuppression(
                "CA1822",
                "Member may use generate code that references non-static members.",
                [SymbolKind.Method, SymbolKind.Property] ),

            // IDE0031 - Use null propagation
            new WellKnownTemplateWarningSuppression(
                "IDE0031",
                "Null propagation of compile-time variables to members returning a run-time value is not supported.",
                _anyTemplate ) { RequiresBody = true },

            // IDE0053 - Use expression body for lambdas
            new WellKnownTemplateWarningSuppression( "IDE0053", "Lambda expressions returning a dynamic type are not supported in templates.", _anyTemplate )
            {
                RequiresBody = true
            },

            // IDE1005 - Use conditional delegate call
            new WellKnownTemplateWarningSuppression( "IDE1005", "Lambda expressions returning a dynamic type are not supported in templates.", _anyTemplate )
            {
                RequiresBody = true
            },

            // IDE0059 - Unnecessary assignment of a value
            new WellKnownTemplateWarningSuppression(
                "IDE0059",
                "Parameters in a contract or initializer template are likely to be used by the overridden member.",
                _anyTemplate ) { RequiresBody = true },

            // CS0628 - New protected member declared in sealed type
            new WellKnownTemplateWarningSuppression( "CS0628", "The target type of the aspect may be non-sealed.", _anyTemplate ),

            // CS8618: Non-nullable property 'property' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
            new WellKnownTemplateWarningSuppression(
                "CS8618",
                "A template property may be initialized by the target code.",
                [SymbolKind.Property, SymbolKind.Field, SymbolKind.Event] ) { AppliesToConstructor = true },

            // CS0169: Field is never used
            new WellKnownTemplateWarningSuppression( "CS0169", "A template field may be used by the target code.", _anyTemplate ),

            // CS0649 - Field is never assigned
            new WellKnownTemplateWarningSuppression(
                "CS0649",
                "A template field may be assigned by the target code.",
                [SymbolKind.Field, SymbolKind.Property, SymbolKind.Event] ),

            // IDE0044: Add readonly modifier
            new WellKnownTemplateWarningSuppression( "IDE0044", "A template field may be set by target code.", [SymbolKind.Field] ),

            // CS9113: Parameter is unread.
            new WellKnownTemplateWarningSuppression( "CS9113", "A template parameter may be read by target code.", [SymbolKind.Method] )
        }.ToDictionary( d => d.DiagnosticId, d => d );
    
    // NOTE: Also add suppressions to Metalama.Testing.AspectTesting.props when updating this file.
}