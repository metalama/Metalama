// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Aspects;

/// <summary>
/// Options controlling how an aspect is suggested in the IDE's code refactoring menu.
/// </summary>
/// <param name="SuggestAsAddAttribute">Whether the aspect should be suggested when adding an attribute.</param>
/// <param name="AddAttributeSuggestionTitle">The title for the "Add attribute" suggestion.</param>
/// <param name="SuggestAsLiveTemplate">Whether the aspect should be suggested as a live template.</param>
/// <param name="LiveTemplateSuggestionTitle">The title for the live template suggestion.</param>
/// <seealso cref="EditorExperienceAttribute"/>
/// <seealso cref="IAspectClass.EditorExperienceOptions"/>
[CompileTime]
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