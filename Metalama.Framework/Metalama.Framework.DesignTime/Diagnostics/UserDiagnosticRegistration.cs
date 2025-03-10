// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.Diagnostics;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;

namespace Metalama.Framework.DesignTime.Diagnostics
{
    /// <summary>
    /// Represents a JSON-serializable user diagnostic for <see cref="UserDiagnosticsConfiguration"/>.
    /// </summary>
    [JsonObject]
    public sealed class UserDiagnosticRegistration
    {
        [JsonConstructor]
        public UserDiagnosticRegistration( string id, DiagnosticSeverity severity, string category, string title )
        {
            this.Severity = severity;
            this.Id = id;
            this.Category = category;
            this.Title = title;
        }

        public UserDiagnosticRegistration( IDiagnosticDefinition descriptor )
        {
            this.Id = descriptor.Id;
            this.Severity = descriptor.Severity.ToRoslynSeverity();
            this.Category = descriptor.Category;
            this.Title = "A Metalama user diagnostic.";
        }

        public DiagnosticDescriptor DiagnosticDescriptor() => new( this.Id, this.Title, "", this.Category, this.Severity, true );

        /// <summary>
        /// Gets the severity of the diagnostic.
        /// </summary>
        public DiagnosticSeverity Severity { get; }

        /// <summary>
        /// Gets an unique identifier for the diagnostic (e.g. <c>MY001</c>).
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the category of the diagnostic (e.g. your product name).
        /// </summary>
        public string Category { get; }

        /// <summary>
        /// Gets a short title describing the diagnostic. This title is typically described in the solution explorer of the IDE
        /// and does not contain formatting string parameters.
        /// </summary>
        public string Title { get; }
    }
}