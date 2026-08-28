// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Analyzers
{
    public partial class ImmutableContractAnalyzer
    {
        /// <summary>
        /// The full metadata name of the interface that every advice attribute implements.
        /// </summary>
        /// <remarks>
        /// One name is enough, and enumerating <c>TemplateAttribute</c>, <c>IntroduceAttribute</c>,
        /// <c>InterfaceMemberAttribute</c>, <c>DeclarativeAdviceAttribute</c> and <c>IntroduceDependencyAttribute</c>
        /// would be both longer and wrong, because it would miss the advice attributes that a user defines. All of
        /// them reach this interface, and <c>ImmutableTemplateExemptionTests</c> asserts that they still do, so that
        /// a refactoring of that hierarchy fails a test rather than silently disabling the exemption.
        /// </remarks>
        private const string _adviceAttributeMetadataName = "Metalama.Framework.Advising.IAdviceAttribute";

        /// <summary>
        /// Determines whether a member is advice rather than state.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A member marked <c>[Template]</c>, <c>[Introduce]</c>, <c>[InterfaceMember]</c>,
        /// <c>[IntroduceDependency]</c> or with a declarative advice attribute is code that the aspect injects into
        /// its target. It is not state of the aspect, it is routinely written as <c>{ get; set; }</c> or as a
        /// writeable field, and the aspect never reads it. Reporting it would be wrong in every case.
        /// </para>
        /// <para>
        /// The override and interface walks below run only when a member is about to be reported, so they cost
        /// nothing in the ordinary case. They are needed because
        /// <c>OverrideFieldOrPropertyAspect.OverrideProperty</c> is declared <c>[Template] public abstract dynamic?
        /// { get; set; }</c> and a user override does not repeat the attribute.
        /// </para>
        /// </remarks>
        private static bool IsAdviceMember( ISymbol member )
        {
            if ( HasAdviceAttribute( member ) )
            {
                return true;
            }

            switch ( member )
            {
                case IPropertySymbol property:
                    foreach ( var implemented in property.ExplicitInterfaceImplementations )
                    {
                        if ( HasAdviceAttribute( implemented ) )
                        {
                            return true;
                        }
                    }

                    for ( var overridden = property.OverriddenProperty; overridden != null; overridden = overridden.OverriddenProperty )
                    {
                        if ( HasAdviceAttribute( overridden ) )
                        {
                            return true;
                        }
                    }

                    return ImplementsAdviceInterfaceMember( property );

                case IFieldSymbol:
                    return false;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Determines whether a property implicitly implements an interface member that carries an advice attribute.
        /// </summary>
        private static bool ImplementsAdviceInterfaceMember( IPropertySymbol property )
        {
            var containingType = property.ContainingType;

            if ( containingType == null )
            {
                return false;
            }

            foreach ( var interfaceType in containingType.AllInterfaces )
            {
                foreach ( var interfaceMember in interfaceType.GetMembers( property.Name ) )
                {
                    if ( interfaceMember is not IPropertySymbol interfaceProperty
                         || !HasAdviceAttribute( interfaceProperty ) )
                    {
                        continue;
                    }

                    if ( SymbolEqualityComparer.Default.Equals(
                            containingType.FindImplementationForInterfaceMember( interfaceProperty ),
                            property ) )
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool HasAdviceAttribute( ISymbol member )
        {
            foreach ( var attribute in member.GetAttributes() )
            {
                if ( attribute.AttributeClass is not { } attributeClass )
                {
                    continue;
                }

                foreach ( var interfaceType in attributeClass.AllInterfaces )
                {
                    if ( SymbolFacts.GetFullMetadataName( interfaceType ) == _adviceAttributeMetadataName )
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
