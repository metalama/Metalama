// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using System;
using System.Reflection;
using MethodKind = Metalama.Framework.Code.MethodKind;
using RefKind = Metalama.Framework.Code.RefKind;
using TypedConstant = Metalama.Framework.Code.TypedConstant;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Builders;

internal partial class AccessorBuilder
{
    private sealed class IndexerParameterBuilder( AccessorBuilder accessor, int? index )
        : BaseParameterBuilder( accessor.Compilation, accessor.AspectLayerInstance )
    {
        private IndexerBuilder Indexer => (IndexerBuilder) accessor._containingMember;

        public override int Index
            => (accessor.MethodKind, _index: index) switch
            {
                (MethodKind.PropertySet, null) => this.Indexer.Parameters.Count,
                _ => index.AssertNotNull()
            };

        public override TypedConstant? DefaultValue
        {
            get
                => accessor.MethodKind switch
                {
                    MethodKind.PropertySet when index == null => null,
                    _ => this.Indexer.Parameters[index.AssertNotNull()].DefaultValue
                };

            set
                => throw new NotSupportedException(
                    $"Setting the default value of indexer accessor {accessor} parameter {this.Index} is not supported. Set the default value on the indexer parameter instead." );
        }

        public override IType Type
        {
            get
                => accessor.MethodKind switch
                {
                    MethodKind.PropertySet when index == null => this.Indexer.Type,
                    _ => this.Indexer.Parameters[index.AssertNotNull()].Type
                };

            set
                => throw new NotSupportedException(
                    $"Setting the type of indexer accessor {accessor} parameter {this.Index} is not supported. Set the type on the indexer parameter instead." );
        }

        public override RefKind RefKind
        {
            get
                => accessor.MethodKind switch
                {
                    MethodKind.PropertySet when index == null => this.Indexer.RefKind,
                    _ => this.Indexer.Parameters[index.AssertNotNull()].RefKind
                };

            set
                => throw new NotSupportedException(
                    $"Setting the ref kind of indexer accessor {accessor} parameter {this.Index} is not supported. Set the ref kind on the indexer parameter instead." );
        }

        public override bool IsParams
        {
            get
                => accessor.MethodKind switch
                {
                    MethodKind.PropertySet when index == null => false,
                    _ => this.Indexer.Parameters[index.AssertNotNull()].IsParams
                };
            set
                => throw new NotSupportedException(
                    $"Setting the 'params' modifier of indexer accessor {accessor} parameter {this.Index} is not supported. Set the params modifier on the indexer parameter instead." );
        }

        public override bool IsThis
        {
            get => false;
            set => throw new NotSupportedException( "Cannot set the 'this' modifier on an indexer parameter." );
        }

        public override string Name
        {
            get
                => accessor.MethodKind switch
                {
                    MethodKind.PropertySet when index == null => "value",
                    _ => this.Indexer.Parameters[index.AssertNotNull()].Name
                };

            set
                => throw new NotSupportedException(
                    $"Setting the name of indexer accessor {accessor} parameter {this.Index} is not supported. Set the name on the indexer parameter instead." );
        }

        public override IHasParameters DeclaringMember => accessor;

        public override bool IsReturnParameter => false;

        public override DeclarationKind DeclarationKind => DeclarationKind.Parameter;

        public override bool CanBeInherited => ((IDeclarationImpl) this.DeclaringMember).CanBeInherited;

        public override ParameterInfo ToParameterInfo()
        {
            throw new NotImplementedException();
        }
    }
}