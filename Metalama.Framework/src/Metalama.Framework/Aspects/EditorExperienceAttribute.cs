// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Eligibility;
using System;

namespace Metalama.Framework.Aspects;

/// <summary>
/// Configures how an aspect is presented in the IDE's code refactoring menu, including options for applying the aspect
/// as a live template or as a custom attribute. This attribute is applied to aspect classes to control their editor experience.
/// </summary>
/// <remarks>
/// <para>
/// Use this attribute on aspect classes (types implementing <see cref="IAspect"/>) to customize how the IDE
/// suggests and displays the aspect to users. You can control whether the aspect appears in code refactoring menus
/// and customize the menu item titles.
/// </para>
/// <para>
/// Two modes of aspect application are supported:
/// </para>
/// <list type="bullet">
/// <item><description><b>Live Template:</b> The aspect is applied directly to the source code, transforming the code itself. Enable this with <see cref="SuggestAsLiveTemplate"/>.</description></item>
/// <item><description><b>Custom Attribute:</b> The aspect is applied by adding a custom attribute to the target declaration. Enable this with <see cref="SuggestAsAddAttribute"/>.</description></item>
/// </list>
/// <para>
/// For both modes, the aspect class must have a default constructor, and the aspect's eligibility (see <see cref="IEligible{T}.BuildEligibility"/>)
/// determines which code elements can have the aspect applied.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [EditorExperience(SuggestAsLiveTemplate = true, LiveTemplateSuggestionTitle = "Logging|Add Method Logging")]
/// public class LogAttribute : OverrideMethodAspect
/// {
///     // Aspect implementation
/// }
/// </code>
/// </example>
/// <seealso cref="IAspect"/>
/// <seealso cref="IEligible{T}"/>
/// <seealso cref="EditorExperienceOptions"/>
/// <seealso href="@live-template"/>
/// <seealso href="@building-ide-interactions"/>
[AttributeUsage( AttributeTargets.Class )]
[CompileTime]
[PublicAPI]
public sealed class EditorExperienceAttribute : Attribute
{
    internal EditorExperienceOptions Options { get; private set; } = EditorExperienceOptions.Default;

    /// <summary>
    /// Gets or sets a value indicating whether the code refactoring menu should offer the possibility to apply this aspect as a live template, i.e., as an action that causes the aspect to applied to
    /// the source code itself. This property is <c>false</c> by default. The property is ignored if the aspect class does not have a default constructor. The eligibility
    /// of the aspect for the <see cref="EligibleScenarios.LiveTemplate"/> scenario is taken into account. See <see cref="IEligible{T}.BuildEligibility"/> for details.
    /// </summary>
    public bool SuggestAsLiveTemplate
    {
        get => this.Options.SuggestAsLiveTemplate.GetValueOrDefault();
        set => this.Options = this.Options with { SuggestAsLiveTemplate = value };
    }

    /// <summary>
    /// Gets or sets the title of the code refactoring menu item that applies the aspect as a live template. By default, the title is <c>Apply Foo</c> if the aspect class is named <c>FooAttribute</c>.
    /// To organize several aspects into sub-menus, use the vertical pipe (<c>|</c>) to separate the different menu levels.
    /// </summary>
    public string? LiveTemplateSuggestionTitle
    {
        get => this.Options.LiveTemplateSuggestionTitle;
        set => this.Options = this.Options with { LiveTemplateSuggestionTitle = value };
    }

    /// <summary>
    /// Gets or sets a value indicating whether the code refactoring menu should offer the possibility to apply this aspect as a custom attribute. This property is <c>false</c> by default.
    /// The property is ignored if the aspect class does not have a default constructor. The eligibility
    /// of the aspect for the <see cref="EligibleScenarios.Default"/> or <see cref="EligibleScenarios.Inheritance"/> scenario is taken into account. See <see cref="IEligible{T}.BuildEligibility"/> for details.
    /// </summary>
    public bool SuggestAsAddAttribute
    {
        get => this.Options.SuggestAsAddAttribute.GetValueOrDefault( true );
        set => this.Options = this.Options with { SuggestAsAddAttribute = value };
    }

    /// <summary>
    /// Gets or sets the title of the code refactoring menu item that applies the aspect as a custom attribute. By default, the title is <c>Add [Foo]</c> if the aspect class is named <c>FooAttribute</c>.
    /// To organize several aspects into sub-menus, use the vertical pipe (<c>|</c>) to separate the different menu levels.
    /// </summary>
    public string? AddAttributeSuggestionTitle
    {
        get => this.Options.AddAttributeSuggestionTitle;
        set => this.Options = this.Options with { AddAttributeSuggestionTitle = value };
    }
}