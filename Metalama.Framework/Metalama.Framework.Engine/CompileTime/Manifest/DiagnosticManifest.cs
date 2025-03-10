// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Diagnostics;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.CompileTime.Manifest
{
    /// <summary>
    /// Exposes the list of diagnostics and suppressions defined in the project.
    /// </summary>
    [JsonObject]
    public sealed class DiagnosticManifest
    {
        [JsonConstructor]
        public DiagnosticManifest(
            ImmutableDictionary<string, IDiagnosticDefinition> diagnosticDefinitions,
            ImmutableDictionary<string, SuppressionDefinition> suppressionDefinitions )
        {
            this.DiagnosticDefinitions = diagnosticDefinitions;
            this.SuppressionDefinitions = suppressionDefinitions;
        }

        [JsonProperty]
        public ImmutableDictionary<string, IDiagnosticDefinition> DiagnosticDefinitions { get; }

        [JsonProperty]
        public ImmutableDictionary<string, SuppressionDefinition> SuppressionDefinitions { get; }

        public bool IsEmpty => this.DiagnosticDefinitions.IsEmpty && this.SuppressionDefinitions.IsEmpty;

        public static DiagnosticManifest Empty { get; } = new( ImmutableArray<IDiagnosticDefinition>.Empty, ImmutableArray<SuppressionDefinition>.Empty );

        public DiagnosticManifest( IEnumerable<IDiagnosticDefinition> diagnosticDescriptions, IEnumerable<SuppressionDefinition> suppressionDescriptions )
        {
            var diagnosticBuilder = ImmutableDictionary.CreateBuilder<string, IDiagnosticDefinition>( StringComparer.OrdinalIgnoreCase );

            foreach ( var description in diagnosticDescriptions )
            {
                diagnosticBuilder[description.Id] = description;
            }

            this.DiagnosticDefinitions = diagnosticBuilder.ToImmutable();

            var suppressionBuilder = ImmutableDictionary.CreateBuilder<string, SuppressionDefinition>( StringComparer.OrdinalIgnoreCase );

            foreach ( var suppression in suppressionDescriptions )
            {
                suppressionBuilder[suppression.SuppressedDiagnosticId] = suppression;
            }

            this.SuppressionDefinitions = suppressionBuilder.ToImmutable();
        }

        public DiagnosticManifest( IReadOnlyCollection<DiagnosticManifest> items ) : this(
            items.SelectMany( i => i.DiagnosticDefinitions.Values ),
            items.SelectMany( i => i.SuppressionDefinitions.Values ) ) { }

        public bool DefinesDiagnostic( string id ) => this.DiagnosticDefinitions.ContainsKey( id );

        public bool DefinesSuppression( string id ) => this.SuppressionDefinitions.ContainsKey( id );

        public DiagnosticManifest Union( DiagnosticManifest other )
        {
            if ( this.IsEmpty )
            {
                return other;
            }
            else if ( other.IsEmpty )
            {
                return this;
            }
            else
            {
                return new DiagnosticManifest( [this, other] );
            }
        }

        public DiagnosticManifest Add( IDiagnosticDefinition definition )
            => new( this.DiagnosticDefinitions.Add( definition.Id, definition ), this.SuppressionDefinitions );
    }
}