// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Framework.Engine.Templating.Expressions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace Metalama.Framework.Engine.Templating
{
    internal sealed partial class TemplateExpansionContext
    {
        private sealed class ProceedUserExpression : UserExpression
        {
            private readonly TemplateExpansionContext _parent;
            private readonly string _methodName;

            public ProceedUserExpression( string methodName, TemplateExpansionContext parent )
            {
                this._methodName = methodName;
                this._parent = parent;
            }

            protected override ExpressionSyntax ToSyntax( SyntaxSerializationContext syntaxSerializationContext, IType? targetType = null )
            {
                this.Validate();

                return this._parent._proceedExpressionProvider!( this._parent._methodTemplate!.EffectiveTemplateKind )
                    .ToTypedExpressionSyntax( syntaxSerializationContext, targetType )
                    .Syntax;
            }

            private void Validate()
            {
                switch ( this._parent.MetaApi.Target.Declaration )
                {
                    case IMethod targetMethod:
                        var isValid = this._methodName switch
                        {
                            nameof(meta.Proceed) => true,
                            nameof(meta.ProceedAsync) => targetMethod.GetAsyncInfoImpl().IsAwaitableOrVoid,
                            nameof(meta.ProceedEnumerable) => targetMethod.GetIteratorInfoImpl().EnumerableKind is EnumerableKind.IEnumerable or EnumerableKind
                                .UntypedIEnumerable,
                            nameof(meta.ProceedEnumerator) => targetMethod.GetIteratorInfoImpl().EnumerableKind is EnumerableKind.IEnumerator or EnumerableKind
                                .UntypedIEnumerator,
                            "ProceedAsyncEnumerable" => targetMethod.GetIteratorInfoImpl().EnumerableKind is EnumerableKind.IAsyncEnumerable,
                            "ProceedAsyncEnumerator" => targetMethod.GetIteratorInfoImpl().EnumerableKind is EnumerableKind.IAsyncEnumerator,
                            _ => throw new ArgumentOutOfRangeException()
                        };

                        if ( !isValid )
                        {
                            throw TemplatingDiagnosticDescriptors.CannotUseSpecificProceedInThisContext.CreateException( (this._methodName, targetMethod) );
                        }

                        break;
                }
            }

            public override IType Type => this._parent._proceedExpressionProvider!( this._parent._methodTemplate!.EffectiveTemplateKind ).Type;

            protected override string ToStringCore() => $"meta.{this._methodName}()";
        }
    }
}