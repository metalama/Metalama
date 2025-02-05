// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Aspects;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.Templating.Expressions
{
    /// <summary>
    /// An <see cref="UserExpression"/> that can be always be used in a <see cref="MemberAccessExpressionSyntax"/>,
    /// but not necessarily as a value itself. This is used to represent <see cref="meta.This"/>. The value represents the current value
    /// or the current type and can be used to allow access to instance or static members.
    /// </summary>
    internal abstract class UserReceiver : UserExpression
    {
        public AspectReferenceSpecification AspectReferenceSpecification { get; }

        protected UserReceiver( in AspectReferenceSpecification aspectReferenceSpecification )
        {
            this.AspectReferenceSpecification = aspectReferenceSpecification;
        }

        public abstract TypedExpressionSyntaxImpl CreateMemberAccessExpression( string member );

        protected abstract UserReceiver WithAspectReferenceSpecification( AspectReferenceSpecification spec );

        public UserReceiver WithAspectReferenceOrder( AspectReferenceOrder order )
        {
            if ( order == this.AspectReferenceSpecification.Order )
            {
                return this;
            }

            return this.WithAspectReferenceSpecification( this.AspectReferenceSpecification.WithOrder( order ) );
        }
    }
}