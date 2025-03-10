// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Contracts.CodeLens;
using Newtonsoft.Json;

namespace Metalama.Framework.DesignTime.CodeLens;

[JsonObject]
public sealed class CodeLensSummary : ICodeLensSummary
{
    public static CodeLensSummary NotAvailable { get; } = new( "-" );

    public static CodeLensSummary NoAspect { get; } = new( "no aspect" );

    [JsonConstructor]
    public CodeLensSummary( string description, string? tooltipText = null )
    {
        this.Description = description;
        this.TooltipText = tooltipText;
    }

    public string Description { get; }

    public string? TooltipText { get; }
}