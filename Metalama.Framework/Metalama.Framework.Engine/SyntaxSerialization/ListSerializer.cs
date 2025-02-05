// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.SyntaxSerialization
{
    internal sealed class ListSerializer : ObjectSerializer
    {
        public override ExpressionSyntax Serialize( object obj, SyntaxSerializationContext serializationContext )
        {
            var serializedItems = new List<ExpressionSyntax>();

            foreach ( var item in (IEnumerable) obj )
            {
                serializedItems.Add( this.Service.Serialize( item, serializationContext ) );
            }

            return ObjectCreationExpression(
                serializationContext.GetTypeSyntax( obj.GetType() ),
                default,
                InitializerExpression( SyntaxKind.CollectionInitializerExpression, SeparatedList( serializedItems ) ) );
        }

        public override Type InputType => typeof(IEnumerable<>);

        public override Type OutputType => typeof(List<>);

        public override int Priority => 1;

        public ListSerializer( SyntaxSerializationService service ) : base( service ) { }

        protected override ImmutableArray<Type> AdditionalSupportedTypes => ImmutableArray.Create( typeof(IReadOnlyList<>), typeof(IReadOnlyCollection<>) );
    }
}