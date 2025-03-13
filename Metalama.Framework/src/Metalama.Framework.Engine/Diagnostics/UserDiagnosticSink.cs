// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Metalama.Framework.Engine.Diagnostics
{
    /// <summary>
    /// Implements the user-level <see cref="IDiagnosticSink"/> interface
    /// and maps user-level diagnostics into Roslyn <see cref="Diagnostic"/>.
    /// </summary>
    public sealed class UserDiagnosticSink : IUserDiagnosticSink
    {
        private readonly UserDiagnosticRegistry _registry;
        private readonly IDiagnosticExtensionPolicy _policy;
        private ConcurrentLinkedList<Diagnostic>? _diagnostics;
        private ConcurrentLinkedList<ScopedSuppression>? _suppressions;
        private ConcurrentLinkedList<IDiagnosticExtension>? _extensions;
        private int _errorCount;
        private bool _frozen;

        internal int ErrorCount => this._errorCount;

        internal bool IsEmpty
        {
            get
            {
                if ( this._diagnostics is { Count: > 0 } )
                {
                    return false;
                }

                if ( this._suppressions is { Count: > 0 } )
                {
                    return false;
                }

                if ( this._extensions is { Count: > 0 } )
                {
                    return false;
                }

                return true;
            }
        }

        public UserDiagnosticSink( ProjectServiceProvider serviceProvider )
        {
            this._registry = serviceProvider.GetRequiredService<UserDiagnosticRegistry>();
            this._policy = serviceProvider.GetRequiredService<IDiagnosticExtensionPolicy>();
        }

        [Conditional( "DEBUG" )]
        private void AssertNotFrozen()
        {
            if ( this._frozen )
            {
                throw new InvalidOperationException( $"The {nameof(UserDiagnosticSink)} has already been frozen." );
            }
        }

        internal void Reset()
        {
            this._diagnostics = null;
            this._suppressions = null;
            this._extensions = null;
            this._frozen = false;
        }

        public void Report( Diagnostic diagnostic )
        {
            this.AssertNotFrozen();
            LazyInitializer.EnsureInitialized( ref this._diagnostics ).Add( diagnostic );

            if ( diagnostic.Severity == DiagnosticSeverity.Error )
            {
                Interlocked.Increment( ref this._errorCount );
            }
        }

        /// <summary>
        /// Returns a string containing all code fix titles and captures the code fixes if we should.  
        /// </summary>
        private ImmutableDictionary<string, string?> ProcessExtensions(
            IDiagnosticDefinition diagnosticDefinition,
            Location? location,
            ImmutableArray<IDiagnosticExtension> extensions )
        {
            var properties = ImmutableDictionary<string, string?>.Empty;

            if ( !extensions.IsDefaultOrEmpty )
            {
                foreach ( var extension in extensions )
                {
                    var handling = this._policy.GetHandling( diagnosticDefinition, location, extension );

                    if ( handling == DiagnosticExtensionHandling.None )
                    {
                        continue;
                    }

                    if ( handling == DiagnosticExtensionHandling.Process )
                    {
                        // Store the code fixes only if we should.

                        LazyInitializer.EnsureInitialized( ref this._extensions )
                            .Add( extension );
                    }

                    properties = extension.ConfigureProperties( properties );
                }
            }

            return properties;
        }

        private void Suppress( ScopedSuppression suppression )
        {
            this.AssertNotFrozen();
            LazyInitializer.EnsureInitialized( ref this._suppressions ).Add( suppression );
        }

        internal void Suppress( IEnumerable<ScopedSuppression> suppressions )
        {
            foreach ( var suppression in suppressions )
            {
                this.Suppress( suppression );
            }
        }

        internal void AddExtensions( ImmutableArray<IDiagnosticExtension> extensions )
        {
            if ( !extensions.IsDefaultOrEmpty )
            {
                LazyInitializer.EnsureInitialized( ref this._extensions );

                foreach ( var extension in extensions )
                {
                    this._extensions.Add( extension );
                }
            }
        }

        internal const string DeduplicationPropertyKey = "Metalama.Deduplication";

        public ImmutableUserDiagnosticList ToImmutable()
        {
            this._frozen = true;

            ImmutableArray<Diagnostic>? immutableDiagnostics = null;

            var diagnostics = this._diagnostics;

            if ( diagnostics != null )
            {
                HashSet<(string DiagnosticId, string DeduplicationKey)>? deduplicatedDiagnostics = null;
                var arrayBuilder = ImmutableArray.CreateBuilder<Diagnostic>( diagnostics.Count );

                var orderedDiagnostics = diagnostics.OrderBy( d => d.Location.SourceTree?.FilePath )
                    .ThenBy( d => d.Location.SourceSpan )
                    .ThenBy( d => d.GetLocalizedMessage() );

                foreach ( var diagnostic in orderedDiagnostics )
                {
                    if ( diagnostic.Properties.TryGetValue( DeduplicationPropertyKey, out var deduplicationKey )
                         && deduplicationKey != null )
                    {
                        deduplicatedDiagnostics ??= new HashSet<(string DiagnosticId, string DeduplicationKey)>();

                        if ( !deduplicatedDiagnostics.Add( (diagnostic.Id, deduplicationKey) ) )
                        {
                            continue;
                        }
                    }

                    arrayBuilder.Add( diagnostic );
                }

                immutableDiagnostics = arrayBuilder.ToImmutable();
            }

            return new ImmutableUserDiagnosticList( immutableDiagnostics, this._suppressions?.ToImmutableArray(), this._extensions?.ToImmutableArray() );
        }

        public override string ToString()
            => $"Diagnostics={this._diagnostics?.Count ?? 0}, Suppressions={this._suppressions?.Count ?? 0}, CodeFixes={this._extensions?.Count ?? 0}";

        private void ValidateUserReport( IDiagnosticDefinition definition )
        {
            if ( !this._registry.IsRegistered( definition ) )
            {
                throw new InvalidOperationException(
                    $"The aspect cannot report the diagnostic {definition.Id} because the DiagnosticDefinition is not declared as a static field or property of a compile-time class." );
            }
        }

        private void ValidateSuppressionDefinition( SuppressionDefinition definition )
        {
            if ( !this._registry.IsRegistered( definition ) )
            {
                throw new InvalidOperationException(
                    $"The aspect cannot suppress the diagnostic {definition.SuppressedDiagnosticId} because the SuppressionDefinition is not declared as a static field or property of the aspect class." );
            }
        }

        public void Report( IDiagnostic diagnostic, IDiagnosticLocation? location, IDiagnosticSource source )
        {
            this.ValidateUserReport( diagnostic.Definition );

            var resolvedLocation = location.GetDiagnosticLocation();
            var extensionProperties = this.ProcessExtensions( diagnostic.Definition, resolvedLocation, diagnostic.Extensions );

            this.Report(
                diagnostic.Definition.CreateRoslynDiagnosticNonGeneric( resolvedLocation, diagnostic.Arguments, source, properties: extensionProperties ) );
        }

        public void Suppress( ISuppression suppression, IDeclaration scope, IDiagnosticSource source )
        {
            this.ValidateSuppressionDefinition( suppression.Definition );

            var symbol = scope.GetSymbol();

            if ( symbol == null )
            {
                // Ignoring any suppression in generated code.
                return;
            }

            this.Suppress( new ScopedSuppression( suppression, symbol ) );
        }
    }
}