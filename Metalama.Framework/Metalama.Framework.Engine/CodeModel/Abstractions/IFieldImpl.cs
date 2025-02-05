// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.CompileTimeContracts;

namespace Metalama.Framework.Engine.CodeModel.Abstractions
{
    internal interface IFieldImpl : IField, IFieldOrPropertyOrIndexerImpl, IUserExpression;
}