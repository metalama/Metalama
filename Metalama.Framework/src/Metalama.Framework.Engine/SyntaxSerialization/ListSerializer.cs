// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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