// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Analyzers.Durability
{
    public partial class DurableContractAnalyzer
    {
        /// <summary>
        /// The full metadata name of the interface implemented by every advice attribute, such as <c>[Template]</c>,
        /// <c>[Introduce]</c>, <c>[InterfaceMember]</c> or <c>[IntroduceDependency]</c>. It is matched by name because
        /// this project deliberately does not reference <c>Metalama.Framework</c>.
        /// </summary>
        private const string _adviceAttributeInterfaceMetadataName = "Metalama.Framework.Advising.IAdviceAttribute";

        /// <summary>
        /// Determines whether a field or property is advice rather than state of the type that declares it.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A member marked with an advice attribute describes code that an aspect injects into its target. The aspect
        /// instance does not hold its value: an introduced field exists only in the target type, at run time. Judging
        /// its declared type by the durable contract would report a member that is never stored across compilations,
        /// and whose type is typically run-time-only, such as a logger introduced by <c>[IntroduceDependency]</c>.
        /// </para>
        /// <para>
        /// This is the counterpart of <c>ImmutabilityValidator.IsAdviceMember</c> in <c>Metalama.Framework.Engine</c>,
        /// which uses the template classifier. The classifier is not available to a Roslyn analyzer, so the advice
        /// attribute is looked for on the member, on the members it overrides, and on the interface properties it
        /// implements, explicitly or implicitly.
        /// </para>
        /// </remarks>
        private static bool IsAdviceMember( ISymbol member )
        {
            for ( var current = member; current != null; current = GetOverriddenMember( current ) )
            {
                if ( HasAdviceAttribute( current ) )
                {
                    return true;
                }
            }

            if ( member is not IPropertySymbol { ContainingType: { } containingType } property )
            {
                return false;
            }

            foreach ( var implementedProperty in property.ExplicitInterfaceImplementations )
            {
                if ( HasAdviceAttribute( implementedProperty ) )
                {
                    return true;
                }
            }

            foreach ( var interfaceType in containingType.AllInterfaces )
            {
                foreach ( var interfaceMember in interfaceType.GetMembers( property.Name ) )
                {
                    if ( interfaceMember.Kind == SymbolKind.Property
                         && HasAdviceAttribute( interfaceMember )
                         && SymbolEqualityComparer.Default.Equals( containingType.FindImplementationForInterfaceMember( interfaceMember ), property ) )
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static ISymbol? GetOverriddenMember( ISymbol member )
            => member switch
            {
                IPropertySymbol { IsOverride: true } property => property.OverriddenProperty,
                _ => null
            };

        private static bool HasAdviceAttribute( ISymbol member )
        {
            foreach ( var attribute in member.GetAttributes() )
            {
                if ( attribute.AttributeClass is not { } attributeClass )
                {
                    continue;
                }

                foreach ( var implementedInterface in attributeClass.AllInterfaces )
                {
                    if ( implementedInterface.Name == "IAdviceAttribute"
                         && DurabilityContext.GetFullMetadataName( implementedInterface ) == _adviceAttributeInterfaceMetadataName )
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
