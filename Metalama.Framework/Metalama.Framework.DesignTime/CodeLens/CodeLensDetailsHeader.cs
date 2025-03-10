// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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