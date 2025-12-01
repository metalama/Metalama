// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Aspects;

/// <summary>
/// Represents the compile-time options controlling how an aspect is suggested in the IDE's code refactoring menu.
/// </summary>
/// <remarks>
/// <para>
/// This record contains the resolved options that control IDE integration for aspects. While aspect authors
/// typically use <see cref="EditorExperienceAttribute"/> to configure these options declaratively, this record
/// represents the effective options that can be queried via <see cref="IAspectClass.EditorExperienceOptions"/>.
/// </para>
/// <para>
/// The options support two modes of aspect application in the IDE:
/// </para>
/// <list type="bullet">
/// <item><description><b>Live Template:</b> Applies the aspect by transforming the source code directly.</description></item>
/// <item><description><b>Add Attribute:</b> Applies the aspect by adding a custom attribute to the declaration.</description></item>
/// </list>
/// </remarks>
/// <param name="SuggestAsAddAttribute">Whether the aspect should be suggested as an "add attribute" code action. When <c>true</c>,
/// the IDE will offer to apply the aspect by adding it as a custom attribute to the target declaration.</param>
/// <param name="AddAttributeSuggestionTitle">The title for the "Add attribute" menu item. Use the vertical pipe (<c>|</c>)
/// to create nested sub-menus. For example, <c>"Logging|Add Audit Logging"</c> creates a "Logging" submenu.</param>
/// <param name="SuggestAsLiveTemplate">Whether the aspect should be suggested as a live template code action. When <c>true</c>,
/// the IDE will offer to apply the aspect directly to the source code, transforming it in place.</param>
/// <param name="LiveTemplateSuggestionTitle">The title for the live template menu item. Use the vertical pipe (<c>|</c>)
/// to create nested sub-menus.</param>
/// <seealso cref="EditorExperienceAttribute"/>
/// <seealso cref="IAspectClass.EditorExperienceOptions"/>
/// <seealso href="@live-template"/>
/// <seealso href="@building-ide-interactions"/>
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