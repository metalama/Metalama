// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.GenericContexts;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using System;

namespace Metalama.Framework.Engine.CodeModel.Source.ConstructedTypes;

internal sealed partial class TupleTypeImpl
{
    private sealed class TupleElement : SourceField, ITupleElement
    {
        public TupleElement( IFieldSymbol symbol, CompilationModel compilation, GenericContext genericContext, string name, int index ) : base(
            symbol,
            compilation,
            genericContext )
        {
            this.Name = name;
            this.Index = index;
        }

        public override FieldKind FieldKind => FieldKind.TupleElement;

        public override string Name
        {
            get
            {
                this.OnUsingDeclaration();

                return field;
            }
        }

        public int Index { get; }

        public bool HasFriendlyName => this.FieldSymbol.CorrespondingTupleField == null || this.Name != this.FieldSymbol.CorrespondingTupleField.Name;

        [Memo]
        public IField CorrespondingTupleField
            => this.Compilation.Factory.GetField( this.FieldSymbol.CorrespondingTupleField ?? this.FieldSymbol, this.GenericContextForSymbolMapping );

        protected override IFullRef<IField> Ref
            => throw new NotSupportedException( $"Referencing an ITupleElement is not supported. Reference {nameof(this.CorrespondingTupleField)} instead." );
    }
}