// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;
using System.Text.Json.Serialization;

namespace Metalama.Framework.ConfigurationFiles
{
    /// <summary>
    /// Represents a JSON-serializable user diagnostic for <see cref="UserDiagnosticsConfiguration"/>.
    /// </summary>
    /// <remarks>
    /// The type is a record and not a class because it derives from <see cref="ConfigurationObject"/>, which carries
    /// the members of the configuration file that this version of Metalama does not declare, and a class cannot
    /// derive from a record.
    /// </remarks>
    public sealed record UserDiagnosticRegistration : ConfigurationObject
    {
        [JsonConstructor]
        public UserDiagnosticRegistration( string id, int severity, string category, string title )
        {
            this.Severity = severity;
            this.Id = id;
            this.Category = category;
            this.Title = title;
        }

        /// <summary>
        /// Gets the severity of the diagnostic. Maps to a value of in the Roslyn's <c>DiagnosticSeverity</c> enum.
        /// </summary>
        public int Severity { get; }

        /// <summary>
        /// Gets a unique identifier for the diagnostic (e.g. <c>MY001</c>).
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
