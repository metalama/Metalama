// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Diagnostics;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;

namespace Metalama.Framework.Engine.CompileTime.Manifest;

[JsonObject]
internal sealed class CompileTimeDiagnosticManifest
{
    public string Id { get; set; }

    public string Category { get; set; }

    public string Message { get; set; }

    public DiagnosticSeverity Severity { get; set; }

    public DiagnosticSeverity DefaultSeverity { get; set; }

    public bool IsEnabledByDefault { get; set; }

    public int WarningLevel { get; set; }

    public bool IsSuppressed { get; set; }

    public string Title { get; set; }

    public string Description { get; set; }

    public string HelpLinkUri { get; set; }

    public CompileTimeDiagnosticLocationManifest Location { get; set; }

    public IReadOnlyList<CompileTimeDiagnosticLocationManifest> AdditionalLocations { get; set; }

    public IEnumerable<string> CustomTags { get; set; }

    public ImmutableDictionary<string, string?> Properties { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
    public CompileTimeDiagnosticManifest() { }
#pragma warning restore CS8618

    public CompileTimeDiagnosticManifest( Diagnostic diagnostic, Dictionary<string, int> sourceFilePathIndexes )
    {
        this.Id = diagnostic.Id;
        this.Category = diagnostic.Descriptor.Category;
        this.Message = diagnostic.GetLocalizedMessage();
        this.Severity = diagnostic.Severity;
        this.DefaultSeverity = diagnostic.DefaultSeverity;
        this.IsEnabledByDefault = diagnostic.Descriptor.IsEnabledByDefault;
        this.WarningLevel = diagnostic.WarningLevel;
        this.IsSuppressed = diagnostic.IsSuppressed;
#pragma warning disable CA1305 // 'CompileTimeDiagnosticManifest.CompileTimeDiagnosticManifest(Diagnostic)' passes 'CultureInfo.CurrentUICulture' as the 'IFormatProvider' parameter to 'LocalizableString.ToString(IFormatProvider)'.This property returns a culture that is inappropriate for formatting methods.
        this.Title = diagnostic.Descriptor.Title.ToString( CultureInfo.CurrentUICulture );
        this.Description = diagnostic.Descriptor.Description.ToString( CultureInfo.CurrentUICulture );
#pragma warning restore CA1305
        this.HelpLinkUri = diagnostic.Descriptor.HelpLinkUri;
        this.Location = new CompileTimeDiagnosticLocationManifest( diagnostic.Location, sourceFilePathIndexes );

        this.AdditionalLocations =
            diagnostic.AdditionalLocations.SelectAsArray( location => new CompileTimeDiagnosticLocationManifest( location, sourceFilePathIndexes ) );

        this.CustomTags = diagnostic.Descriptor.CustomTags;
        this.Properties = diagnostic.Properties;
    }

    public Diagnostic ToDiagnostic( SyntaxTree[] sourceTrees )
        => Diagnostic.Create(
            this.Id,
            this.Category,
            this.Message,
            this.Severity,
            this.DefaultSeverity,
            this.IsEnabledByDefault,
            this.WarningLevel,
            this.IsSuppressed,
            this.Title,
            this.Description,
            this.HelpLinkUri,
            this.Location.ToLocation( sourceTrees ),
            this.AdditionalLocations.SelectAsReadOnlyList( l => l.ToLocation( sourceTrees ) ),
            this.CustomTags,
            this.Properties );
}