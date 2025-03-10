// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Transformations;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.AdviceImpl.Contracts;

internal sealed class ContractMethodTransformation : ContractBaseTransformation
{
    private readonly IFullRef<IMethod> _targetMethod;

    public ContractMethodTransformation(
        AspectLayerInstance aspectLayerInstance,
        IFullRef<IMethod> targetMethod,
        IFullRef<IParameter> contractTarget,
        ContractDirection contractDirection,
        TemplateMember<IMethod> template,
        IObjectReader templateArguments ) : base(
        aspectLayerInstance,
        contractTarget,
        contractDirection,
        template,
        templateArguments )
    {
        this._targetMethod = targetMethod;
    }

    public override IReadOnlyList<InsertedStatement> GetInsertedStatements( InsertStatementTransformationContext context )
    {
        switch ( this.ContractTarget.GetTarget( this.InitialCompilation ) )
        {
            case IParameter { IsReturnParameter: true } returnValueParam:
                {
                    Invariant.Assert( this.ContractDirection == ContractDirection.Output );

                    var variableName = context.GetReturnValueVariableName();

                    if ( !this.TryExecuteTemplate( context, IdentifierName( variableName ), returnValueParam.Type, out var contractBlock ) )
                    {
                        return Array.Empty<InsertedStatement>();
                    }
                    else
                    {
                        return [new InsertedStatement( contractBlock, returnValueParam, this, InsertedStatementKind.OutputContract )];
                    }
                }

            case IParameter param:
                {
                    Invariant.Assert( this.ContractDirection is ContractDirection.Output or ContractDirection.Input or ContractDirection.Both );

                    bool? inputResult, outputResult;
                    BlockSyntax? inputContractBlock, outputContractBlock;
                    var valueSyntax = IdentifierName( param.Name );

                    if ( this.ContractDirection is ContractDirection.Input or ContractDirection.Both )
                    {
                        Invariant.Assert( param.RefKind is not RefKind.Out );
                        inputResult = this.TryExecuteTemplate( context, valueSyntax, param.Type, out inputContractBlock );
                    }
                    else
                    {
                        inputResult = null;
                        inputContractBlock = null;
                    }

                    if ( this.ContractDirection is ContractDirection.Output or ContractDirection.Both )
                    {
                        Invariant.Assert( param.RefKind is not RefKind.None );
                        outputResult = this.TryExecuteTemplate( context, valueSyntax, param.Type, out outputContractBlock );
                    }
                    else
                    {
                        outputResult = null;
                        outputContractBlock = null;
                    }

                    if ( inputResult == false || outputResult == false )
                    {
                        return Array.Empty<InsertedStatement>();
                    }

                    var statements = new List<InsertedStatement>();

                    if ( inputContractBlock != null )
                    {
                        statements.Add( new InsertedStatement( inputContractBlock, param, this, InsertedStatementKind.InputContract ) );
                    }

                    if ( outputContractBlock != null )
                    {
                        statements.Add( new InsertedStatement( outputContractBlock, param, this, InsertedStatementKind.OutputContract ) );
                    }

                    return statements;
                }

            default:
                throw new AssertionFailedException( $"Unsupported contract target: {this.ContractTarget}" );
        }
    }

    public override IFullRef<IMember> TargetMember => this._targetMethod;
}