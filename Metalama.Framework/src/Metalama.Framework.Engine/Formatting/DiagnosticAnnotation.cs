// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Diagnostics;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable MemberCanBePrivate.Global

#pragma warning disable 8618 // Property Id not initialized.

namespace Metalama.Framework.Engine.Formatting
{
    [JsonObject]
    public sealed class DiagnosticAnnotation
    {
        [JsonConstructor]
        public DiagnosticAnnotation() { }

        public DiagnosticAnnotation( Diagnostic diagnostic )
        {
            this.Id = diagnostic.Id;
            this.Message = diagnostic.GetLocalizedMessage();
            this.Severity = diagnostic.Severity;
        }

        public string Id { get; init; }

        public DiagnosticSeverity Severity { get; init; }

        public string Message { get; init; }

        public string ToJson() => JsonConvert.SerializeObject( this );

        public static DiagnosticAnnotation FromJson( string json ) => JsonConvert.DeserializeObject<DiagnosticAnnotation>( json )!;

        public override string ToString() => $"{this.Severity} {this.Id}: {this.Message}";
    }
}