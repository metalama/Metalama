// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
    /// Custom attribute that can be applied to any member of an aspect class and that means that this member must be introduced to
    /// the target class of the aspect. 
    /// </summary>
    /// <seealso href="@introducing-members"/>
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
        /// Gets or sets the implementation strategy (like <see cref="OverrideStrategy.Override"/>, <see cref="OverrideStrategy.Fail"/> or <see cref="OverrideStrategy.Ignore"/>) when the member is already declared in the target type.
        /// The default value is <see cref="OverrideStrategy.Fail"/>. 
        /// </summary>
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

                            return t is { TypeKind: not (TypeKind.Struct or TypeKind.RecordStruct) } and { IsStatic: false, IsSealed: false };
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
                case { TypeKind: not (TypeKind.Class or TypeKind.Struct or TypeKind.RecordClass or TypeKind.RecordStruct or TypeKind.Interface) }:
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
                    builder.Advice.IntroduceMethod( targetType, templateMemberId, this.Scope, this.WhenExists );

                    break;

                case DeclarationKind.Property:
                    builder.Advice.IntroduceProperty( targetType, templateMemberId, this.Scope, this.WhenExists );

                    break;

                case DeclarationKind.Event:
                    builder.Advice.IntroduceEvent( targetType, templateMemberId, this.Scope, this.WhenExists );

                    break;

                case DeclarationKind.Field:
                    builder.Advice.IntroduceField( targetType, templateMemberId, this.Scope, this.WhenExists );

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