// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Eligibility;
using System;

namespace Metalama.Framework.Aspects;

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