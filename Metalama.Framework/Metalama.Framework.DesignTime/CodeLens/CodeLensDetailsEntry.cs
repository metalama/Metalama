// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Contracts.CodeLens;
using Newtonsoft.Json;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.CodeLens;

[JsonObject]
public sealed class CodeLensDetailsEntry : ICodeLensDetailsEntry
{
    [JsonConstructor]
    public CodeLensDetailsEntry( ImmutableArray<CodeLensDetailsField> fields, string? tooltip = null )
    {
        this.Fields = fields;
        this.Tooltip = tooltip;
    }

    ICodeLensDetailsField[] ICodeLensDetailsEntry.Fields => this.Fields.ToArray<ICodeLensDetailsField>();

    public ImmutableArray<CodeLensDetailsField> Fields { get; }

    public string? Tooltip { get; }
}