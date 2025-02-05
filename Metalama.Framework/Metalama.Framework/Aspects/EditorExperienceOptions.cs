// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.Aspects;

public sealed record EditorExperienceOptions(
    bool? SuggestAsAddAttribute = null,
    string? AddAttributeSuggestionTitle = null,
    bool? SuggestAsLiveTemplate = null,
    string? LiveTemplateSuggestionTitle = null )
{
    internal EditorExperienceOptions Override( EditorExperienceOptions overriding )
        => new(
            AddAttributeSuggestionTitle: overriding.AddAttributeSuggestionTitle ?? this.AddAttributeSuggestionTitle,
            LiveTemplateSuggestionTitle: overriding.LiveTemplateSuggestionTitle ?? this.LiveTemplateSuggestionTitle,
            SuggestAsAddAttribute: overriding.SuggestAsAddAttribute ?? this.SuggestAsAddAttribute,
            SuggestAsLiveTemplate: overriding.SuggestAsLiveTemplate ?? this.SuggestAsLiveTemplate );

    public static EditorExperienceOptions Default { get; } = new();
}