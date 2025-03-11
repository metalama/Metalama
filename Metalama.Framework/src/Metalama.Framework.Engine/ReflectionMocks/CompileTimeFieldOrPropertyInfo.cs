// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Framework.RunTime;

namespace Metalama.Framework.Engine.ReflectionMocks
{
    internal sealed class CompileTimeFieldOrPropertyInfo : FieldOrPropertyInfo, IUserExpression
    {
        public IFieldOrPropertyOrIndexer FieldOrPropertyOrIndexer { get; }

        private CompileTimeFieldOrPropertyInfo( IFieldOrPropertyOrIndexer fieldOrPropertyOrIndexer )
        {
            this.FieldOrPropertyOrIndexer = fieldOrPropertyOrIndexer;
        }

        public static FieldOrPropertyInfo Create( IFieldOrPropertyOrIndexer fieldOrPropertyOrIndexer )
            => new CompileTimeFieldOrPropertyInfo( fieldOrPropertyOrIndexer );

        public bool IsAssignable => false;

        public IType Type => TypeFactory.GetType( typeof(FieldOrPropertyInfo) );

        public RefKind RefKind => RefKind.None;

        public ref object? Value => ref RefHelper.Wrap( this );

        public TypedExpressionSyntax ToTypedExpressionSyntax( ISyntaxGenerationContext syntaxGenerationContext, IType? targetType = null )
            => CompileTimeMocksHelper.ToTypedExpressionSyntax(
                this.FieldOrPropertyOrIndexer,
                typeof(FieldOrPropertyInfo),
                CompileTimeFieldOrPropertyInfoSerializer.SerializeFieldOrProperty,
                syntaxGenerationContext );
    }
}