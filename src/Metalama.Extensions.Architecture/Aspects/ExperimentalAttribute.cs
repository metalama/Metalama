// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Extensions.Validation;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using System;
using System.Linq;

namespace Metalama.Extensions.Architecture.Aspects
{
    /// <summary>
    /// Aspect that, when applied to a declaration, reports a warning when any other declaration tries to use it, unless the using declaration
    /// is also marked as experimental.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.All & ~(AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter | AttributeTargets.Assembly
                                 | AttributeTargets.Module) )]
    [PublicAPI]
    public class ExperimentalAttribute : Attribute, IAspect<IDeclaration>
    {
        public string? Description { get; }

        public ExperimentalAttribute( string? description = null )
        {
            this.Description = description;
        }

        public void BuildAspect( IAspectBuilder<IDeclaration> builder )
        {
            builder.Outbound.ValidateInboundReferences( this.ValidateReference, ReferenceGranularity.Member );
        }

        private void ValidateReference( ReferenceValidationContext context )
        {
            // Declarations contained in an experimental declaration can reference it.
            if ( context.Origin.Type.IsContainedIn( context.Destination.Declaration ) )
            {
                return;
            }

            // An experimental declaration an reference another experimental declaration.
            if ( context.Origin.Declaration.ContainingAncestorsAndSelf()
                .Any( d => d.DeclarationKind != DeclarationKind.Compilation && d.Enhancements().HasAspect<ExperimentalAttribute>() ) )
            {
                return;
            }

            context.Diagnostics.Report(
                ArchitectureDiagnosticDefinitions.ExperimentalApi.WithArguments(
                    (context.Destination.Declaration, context.Destination.Declaration.DeclarationKind, string.IsNullOrEmpty( this.Description ) ? "" : " ",
                     this.Description) ) );
        }

        void IEligible<IDeclaration>.BuildEligibility( IEligibilityBuilder<IDeclaration> builder )
        {
            builder.Convert()
                .When<IMemberOrNamedType>()
                .MustHaveAccessibility( Accessibility.Public, Accessibility.Protected, Accessibility.PrivateProtected, Accessibility.ProtectedInternal );
        }
    }
}