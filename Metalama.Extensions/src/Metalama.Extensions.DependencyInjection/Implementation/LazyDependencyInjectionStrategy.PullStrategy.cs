// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using System;

namespace Metalama.Extensions.DependencyInjection.Implementation;

public partial class LazyDependencyInjectionStrategy
{
    private class PullStrategy : DefaultPullStrategy
    {
        private readonly IField _funcField;
        private readonly INamedType _funcType;

        protected override IType ParameterType => this._funcType;

        public PullStrategy( DependencyProperties properties, IProperty mainProperty, IField funcField ) : base( properties, mainProperty )
        {
            this._funcField = funcField;
            this._funcType = ((INamedType) TypeFactory.GetType( typeof(Func<>) )).WithTypeArguments( mainProperty.Type ).ToNullable();
        }

        protected override IFieldOrProperty AssignedFieldOrProperty => this._funcField;
    }
}