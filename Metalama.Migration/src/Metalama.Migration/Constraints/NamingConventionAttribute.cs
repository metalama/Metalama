// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using PostSharp.Extensibility;
using System;

namespace PostSharp.Constraints
{
    /// <summary>
    /// In Metalama, use <see cref="Metalama.Extensions.Architecture.Aspects.DerivedTypesMustRespectNamingConventionAttribute"/>
    /// </summary>
    [MulticastAttributeUsage( MulticastTargets.Class | MulticastTargets.Interface, Inheritance = MulticastInheritance.Strict )]
    [AttributeUsage( AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Assembly )]
    [PublicAPI]
    public sealed class NamingConventionAttribute : ScalarConstraint
    {
        public NamingConventionAttribute( string pattern )
        {
            throw new NotImplementedException();
        }

        public SeverityType Severity { get; set; } = SeverityType.Warning;

        public override bool ValidateConstraint( object target )
        {
            throw new NotImplementedException();
        }

        public override void ValidateCode( object target )
        {
            throw new NotImplementedException();
        }
    }
}