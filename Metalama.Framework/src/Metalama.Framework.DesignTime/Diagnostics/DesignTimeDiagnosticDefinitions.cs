// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.DesignTime.DiagnosticAnalysis;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Linking;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Framework.Engine.Templating;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.Diagnostics
{
    public sealed class DesignTimeDiagnosticDefinitions
    {
        internal ImmutableDictionary<string, DiagnosticDescriptor> SupportedDiagnosticDescriptors { get; }

        internal ImmutableDictionary<string, DiagnosticDescriptor> UserDiagnosticDescriptors { get; }

        internal ImmutableDictionary<string, SuppressionDescriptor> SupportedSuppressionDescriptors { get; }

        // TODO: Diagnostics from the Premium add-in?

        /// <summary>
        /// Gets the set of <see cref="DiagnosticDescriptor"/> that are defined by Metalama itself.
        /// </summary>
        internal static ImmutableDictionary<string, DiagnosticDescriptor> StandardDiagnosticDescriptors { get; } = new DiagnosticDefinitionDiscoveryService()
            .GetDiagnosticDefinitions(
                typeof(CodeFixHelper),
                typeof(GeneralDiagnosticDescriptors),
                typeof(TemplatingDiagnosticDescriptors),
                typeof(SerializationDiagnosticDescriptors),
                typeof(DesignTimeDiagnosticDescriptors),
                typeof(AttributeDeserializerDiagnostics),
                typeof(AdviceDiagnosticDescriptors),
                typeof(AspectLinkerDiagnosticDescriptors),
                typeof(FrameworkDiagnosticDescriptors) )
            .Select( d => d.ToRoslynDescriptor() )
            .ToImmutableDictionary( d => d.Id, d => d, StringComparer.CurrentCultureIgnoreCase );

        internal DesignTimeDiagnosticDefinitions(
            ImmutableArray<DiagnosticDescriptor> diagnosticDescriptors,
            ImmutableArray<SuppressionDescriptor> suppressionDescriptors )
        {
            // The file may contain system descriptors by mistake. We must remove them otherwise we will have some duplicate key issue.

            this.SupportedDiagnosticDescriptors =
                StandardDiagnosticDescriptors.Values
                    .Concat( diagnosticDescriptors.Where( d => !StandardDiagnosticDescriptors.ContainsKey( d.Id ) ) )
                    .ToImmutableDictionary( d => d.Id, d => d, StringComparer.OrdinalIgnoreCase );

            this.UserDiagnosticDescriptors = diagnosticDescriptors.ToImmutableDictionary( d => d.Id, d => d, StringComparer.OrdinalIgnoreCase );

            this.SupportedSuppressionDescriptors =
                suppressionDescriptors.ToImmutableDictionary( d => d.SuppressedDiagnosticId, d => d, StringComparer.OrdinalIgnoreCase );
        }
    }
}