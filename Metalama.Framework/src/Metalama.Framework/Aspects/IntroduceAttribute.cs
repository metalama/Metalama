// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Eligibility.Implementation;
using System;
using System.Linq;

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// Marks a member in an aspect class to be introduced (added) to the target type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This attribute enables <i>declarative member introduction</i>, the simplest way to add members through aspects.
    /// When applied to a method, property, field, or event in an aspect class, that member is automatically introduced
    /// into the target type when the aspect is applied.
    /// </para>
    /// <para>
    /// The introduced member is based on a T# template - compile-time logic executes to generate the run-time code.
    /// Properties like <see cref="Name"/>, <see cref="Accessibility"/>, and <see cref="IsVirtual"/> allow you to
    /// customize the introduced member's characteristics. When not set, these properties default to the values from
    /// the template member.
    /// </para>
    /// <para>
    /// Use <see cref="WhenExists"/> to control behavior when a member with the same name already exists in the target type.
    /// The default is <see cref="OverrideStrategy.Fail"/>, which reports an error.
    /// </para>
    /// <para>
    /// For programmatic member introduction with more control, use <see cref="AdviserExtensions.IntroduceMethod"/>,
    /// <see cref="AdviserExtensions.IntroduceProperty"/>, <see cref="AdviserExtensions.IntroduceField"/>, or
    /// <see cref="AdviserExtensions.IntroduceEvent"/> in the <see cref="IAspect{T}.BuildAspect"/> method.
    /// </para>
    /// </remarks>
    /// <seealso cref="DeclarativeAdviceAttribute"/>
    /// <seealso cref="ITemplateAttribute"/>
    /// <seealso cref="TemplateAttribute"/>
    /// <seealso cref="IntroductionScope"/>
    /// <seealso cref="OverrideStrategy"/>
    /// <seealso href="@introducing-members"/>
    /// <seealso href="@advising-concepts"/>
    [AttributeUsage( AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Event )]
    [PublicAPI]
    public sealed class IntroduceAttribute : DeclarativeAdviceAttribute, ITemplateAttribute
    {
        private TemplateAttributeProperties _properties = new();

        public string? Name
        {
            get => this._properties.Name;
            set => this._properties = this._properties with { Name = value };
        }

        public Accessibility Accessibility
        {
            get => this._properties.Accessibility.GetValueOrDefault();
            set => this._properties = this._properties with { Accessibility = value };
        }

        public bool IsVirtual
        {
            get => this._properties.IsVirtual.GetValueOrDefault();

            set => this._properties = this._properties with { IsVirtual = value };
        }

        public bool IsSealed
        {
            get => this._properties.IsSealed.GetValueOrDefault();
            set => this._properties = this._properties with { IsSealed = value };
        }

        public bool IsRequired
        {
            get => this._properties.IsRequired.GetValueOrDefault();
            set => this._properties = this._properties with { IsRequired = value };
        }

        public IntroductionScope Scope { get; set; }

        /// <summary>
        /// Gets or sets the strategy to use when a member with the same name already exists in the target type.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The default value is <see cref="OverrideStrategy.Fail"/>, which reports an error when a conflict is detected.
        /// Other options include:
        /// <list type="bullet">
        /// <item><see cref="OverrideStrategy.Override"/>: Replace the existing member with the introduced one</item>
        /// <item><see cref="OverrideStrategy.Ignore"/>: Skip introducing the member without reporting an error</item>
        /// <item><see cref="OverrideStrategy.New"/>: Introduce the member with the <c>new</c> modifier, hiding the existing member</item>
        /// </list>
        /// </para>
        /// </remarks>
        public OverrideStrategy WhenExists { get; set; }

        /// <summary>
        /// Gets or sets the implementation strategy (like <see cref="OverrideStrategy.Override"/>, <see cref="OverrideStrategy.Fail"/> or <see cref="OverrideStrategy.Ignore"/>) when the member is already declared
        /// in a parent class of the target type.
        /// The default value is <see cref="OverrideStrategy.Fail"/>. 
        /// </summary>
        [Obsolete( "Not implemented." )]
        public OverrideStrategy WhenInherited { get; set; }

        public override void BuildAspectEligibility( IEligibilityBuilder<IDeclaration> builder, IMemberOrNamedType adviceMember )
        {
            builder.MustBeInstanceOfType( typeof(IMemberOrNamedType) );

            builder.MustBeExplicitlyDeclared();

            var isEffectivelyInstance =
                (this.Scope, adviceMember.IsStatic) switch
                {
                    (IntroductionScope.Default, false) => true,
                    (IntroductionScope.Instance, _) => true,
                    _ => false
                };

            var isEffectivelyVirtual =
                (this._properties.IsVirtual, (adviceMember as IMember)?.IsVirtual ?? false) switch
                {
                    (null, true) => true,
                    (true, _) => true,
                    _ => false
                };

            // Rules for virtuality and staticity.
            if ( isEffectivelyInstance )
            {
                builder.AddRule(
                    new EligibilityRule<IDeclaration>(
                        EligibleScenarios.Inheritance,
                        x =>
                        {
                            var t = x.GetClosestNamedType();

                            return t is { IsStatic: false };
                        },
                        _ => $"the aspect contains an instance declarative introduction and therefore cannot be applied to static types" ) );
            }

            if ( isEffectivelyVirtual )
            {
                builder.AddRule(
                    new EligibilityRule<IDeclaration>(
                        EligibleScenarios.Inheritance,
                        x =>
                        {
                            var t = x.GetClosestNamedType();

                            return t is { TypeKind: not TypeKind.Struct } and { IsStatic: false, IsSealed: false };
                        },
                        _ => $"the aspect contains an virtual declarative introduction and therefore cannot be applied to sealed types, static types and structs" ) );
            }
        }

        public override void BuildAdvice( IMemberOrNamedType templateMember, string templateMemberId, IAspectBuilder<IDeclaration> builder )
        {
            if ( this.Layer != builder.Layer )
            {
                return;
            }

            INamedType targetType;

            switch ( builder.Target )
            {
                case IMember member:
                    targetType = member.DeclaringType;

                    break;

                case INamedType type:
                    targetType = type;

                    break;

                default:
                    builder.Diagnostics.Report(
                        FrameworkDiagnosticDescriptors.CannotUseIntroduceWithoutDeclaringType.WithArguments(
                            (builder.AspectInstance.AspectClass.ShortName, templateMember.DeclarationKind, builder.Target.DeclarationKind) ) );

                    builder.SkipAspect();

                    return;
            }

            switch ( targetType )
            {
                case { TypeKind: not (TypeKind.Class or TypeKind.Struct or TypeKind.Interface) }:
                    builder.Diagnostics.Report(
                        FrameworkDiagnosticDescriptors.CannotApplyAdviceOnTypeOrItsMembers.WithArguments(
                            (builder.AspectInstance.AspectClass.ShortName, templateMember.DeclarationKind, targetType.TypeKind) ) );

                    builder.SkipAspect();

                    return;
            }

            if ( HasInheritedIntroductionAttribute( templateMember ) )
            {
                // All members that are overrides of introduced members have to be skipped - the template will be selected correctly when binding.
                return;
            }

            switch ( templateMember.DeclarationKind )
            {
                case DeclarationKind.Method:
                    builder.With( targetType ).IntroduceMethod( templateMemberId, this.Scope, this.WhenExists );

                    break;

                case DeclarationKind.Property:
                    builder.With( targetType ).IntroduceProperty( templateMemberId, this.Scope, this.WhenExists );

                    break;

                case DeclarationKind.Event:
                    builder.With( targetType ).IntroduceEvent( templateMemberId, this.Scope, this.WhenExists );

                    break;

                case DeclarationKind.Field:
                    builder.With( targetType ).IntroduceField( templateMemberId, this.Scope, this.WhenExists );

                    break;

                case DeclarationKind.Indexer:
                    throw new NotSupportedException( $"Indexers cannot be introduced declaratively, use programmatic introduction instead." );

                default:
                    throw new InvalidOperationException( $"Don't know how to introduce a {templateMember.DeclarationKind}." );
            }
        }

        TemplateAttributeProperties ITemplateAttribute.Properties => this._properties;

        private static bool HasInheritedIntroductionAttribute( IMemberOrNamedType templateMember )
        {
            return GetNoAttributeCheck( templateMember );

            static bool Get( IMemberOrNamedType templateMember )
            {
                if ( templateMember.Attributes.OfAttributeType( typeof(IntroduceAttribute) ).Any() )
                {
                    return true;
                }
                else
                {
                    return GetNoAttributeCheck( templateMember );
                }
            }

            static bool GetNoAttributeCheck( IMemberOrNamedType templateMember )
            {
                if ( templateMember is IMethod { OverriddenMethod: { } overriddenMethod } )
                {
                    return Get( overriddenMethod );
                }
                else if ( templateMember is IProperty { OverriddenProperty: { } overriddenProperty } )
                {
                    return Get( overriddenProperty );
                }
                else if ( templateMember is IEvent { OverriddenEvent: { } overriddenEvent } )
                {
                    return Get( overriddenEvent );
                }
                else
                {
                    return false;
                }
            }
        }
    }
}