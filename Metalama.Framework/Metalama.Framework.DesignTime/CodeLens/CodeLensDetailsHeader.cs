// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Contracts.CodeLens;
using Newtonsoft.Json;

namespace Metalama.Framework.DesignTime.CodeLens;

[JsonObject]
public sealed class CodeLensDetailsHeader : ICodeLensDetailsHeader
{
    [JsonConstructor]
    public CodeLensDetailsHeader( string displayName, string uniqueName, bool isVisible = true, double width = 0 )
    {
        this.DisplayName = displayName;
        this.IsVisible = isVisible;
        this.UniqueName = uniqueName;
        this.Width = width;
    }

    public string DisplayName { get; }

    public bool IsVisible { get; }

    public string UniqueName { get; }

    public double Width { get; }
}