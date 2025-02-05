// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.SyntaxSerialization
{
    internal abstract class ValueTupleSerializer : ObjectSerializer
    {
        private ValueTupleSerializer( SyntaxSerializationService service ) : base( service ) { }

        private static IEnumerable<object?> GetItems( object obj )
        {
            var type = obj.GetType();
            var arity = type.GetGenericArguments().Length;

            for ( var i = 0; i < arity; i++ )
            {
                var field = type.GetField( $"Item{i + 1}", BindingFlags.Instance | BindingFlags.Public ).AssertNotNull();

                yield return field.GetValue( obj );
            }
        }

        protected abstract Type ValueTupleType { get; }

        public sealed override ExpressionSyntax Serialize( object obj, SyntaxSerializationContext serializationContext )
            => TupleExpression( SeparatedList( GetItems( obj ).Select( o => Argument( this.Service.Serialize( o, serializationContext ) ) ) ) );

        public sealed override Type InputType => this.ValueTupleType;

        public sealed override Type OutputType => this.ValueTupleType;

        internal sealed class Size1 : ValueTupleSerializer
        {
            public Size1( SyntaxSerializationService service ) : base( service ) { }

            protected override Type ValueTupleType => typeof(ValueTuple<>);
        }

        internal sealed class Size2 : ValueTupleSerializer
        {
            public Size2( SyntaxSerializationService service ) : base( service ) { }

            protected override Type ValueTupleType => typeof(ValueTuple<,>);
        }

        internal sealed class Size3 : ValueTupleSerializer
        {
            public Size3( SyntaxSerializationService service ) : base( service ) { }

            protected override Type ValueTupleType => typeof(ValueTuple<,,>);
        }

        internal sealed class Size4 : ValueTupleSerializer
        {
            public Size4( SyntaxSerializationService service ) : base( service ) { }

            protected override Type ValueTupleType => typeof(ValueTuple<,,,>);
        }

        internal sealed class Size5 : ValueTupleSerializer
        {
            public Size5( SyntaxSerializationService service ) : base( service ) { }

            protected override Type ValueTupleType => typeof(ValueTuple<,,,,>);
        }

        internal sealed class Size6 : ValueTupleSerializer
        {
            public Size6( SyntaxSerializationService service ) : base( service ) { }

            protected override Type ValueTupleType => typeof(ValueTuple<,,,,,>);
        }

        internal sealed class Size7 : ValueTupleSerializer
        {
            public Size7( SyntaxSerializationService service ) : base( service ) { }

            protected override Type ValueTupleType => typeof(ValueTuple<,,,,,,>);
        }

        internal sealed class Size8 : ValueTupleSerializer
        {
            public Size8( SyntaxSerializationService service ) : base( service ) { }

            protected override Type ValueTupleType => typeof(ValueTuple<,,,,,,,>);
        }
    }
}