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

internal sealed class ContractIndexerTransformation : ContractBaseTransformation
{
    private readonly IFullRef<IIndexer> _targetIndexer;

    public ContractIndexerTransformation(
        AspectLayerInstance aspectLayerInstance,
        IFullRef<IIndexer> targetIndexer,
        IFullRef<IParameter>? indexerParameter,
        ContractDirection contractDirection,
        TemplateMember<IMethod> template,
        IObjectReader templateArguments ) : base(
        aspectLayerInstance,
        (IFullRef<IDeclaration>?) indexerParameter ?? targetIndexer,
        contractDirection,
        template,
        templateArguments )
    {
        this._targetIndexer = targetIndexer;
    }

    public override IReadOnlyList<InsertedStatement> GetInsertedStatements( InsertStatementTransformationContext context )
    {
        switch ( this.ContractTarget.GetTarget( this.InitialCompilation ) )
        {
            case IIndexer:
                {
                    Invariant.Assert( this.ContractTarget.Equals( this.TargetMember ) );

                    var targetMember = this._targetIndexer.GetTarget( context.FinalCompilation );

                    Invariant.Assert( this.ContractDirection is ContractDirection.Output or ContractDirection.Input or ContractDirection.Both );

                    bool? inputResult, outputResult;
                    BlockSyntax? inputContractBlock, outputContractBlock;

                    if ( this.ContractDirection is ContractDirection.Input or ContractDirection.Both )
                    {
                        Invariant.Assert( targetMember.SetMethod is not null );

                        inputResult = this.TryExecuteTemplate( context, IdentifierName( "value" ), targetMember.Type, out inputContractBlock );
                    }
                    else
                    {
                        inputResult = null;
                        inputContractBlock = null;
                    }

                    if ( this.ContractDirection is ContractDirection.Output or ContractDirection.Both )
                    {
                        Invariant.Assert( targetMember.GetMethod is not null );

                        var returnVariableName = context.GetReturnValueVariableName();

                        outputResult = this.TryExecuteTemplate(
                            context,
                            IdentifierName( returnVariableName ),
                            targetMember.Type,
                            out outputContractBlock );
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
                        statements.Add(
                            new InsertedStatement(
                                inputContractBlock,
                                targetMember.SetMethod.AssertNotNull().Parameters[^1],
                                this,
                                InsertedStatementKind.InputContract ) );
                    }

                    if ( outputContractBlock != null )
                    {
                        statements.Add(
                            new InsertedStatement(
                                outputContractBlock,
                                targetMember.GetMethod.AssertNotNull().ReturnParameter,
                                this,
                                InsertedStatementKind.OutputContract ) );
                    }

                    return statements;
                }

            case IParameter parameter:
                {
                    Invariant.Assert( this.ContractDirection is ContractDirection.Output or ContractDirection.Input or ContractDirection.Both );

                    bool? inputResult, outputResult;
                    BlockSyntax? inputContractBlock, outputContractBlock;
                    var valueSyntax = IdentifierName( parameter.Name );

                    if ( this.ContractDirection is ContractDirection.Input or ContractDirection.Both )
                    {
                        Invariant.Assert( parameter.RefKind is not RefKind.Out );
                        inputResult = this.TryExecuteTemplate( context, valueSyntax, parameter.Type, out inputContractBlock );
                    }
                    else
                    {
                        inputResult = null;
                        inputContractBlock = null;
                    }

                    if ( this.ContractDirection is ContractDirection.Output or ContractDirection.Both )
                    {
                        Invariant.Assert( parameter.RefKind is not RefKind.None );
                        outputResult = this.TryExecuteTemplate( context, valueSyntax, parameter.Type, out outputContractBlock );
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
                        statements.Add( new InsertedStatement( inputContractBlock, parameter, this, InsertedStatementKind.InputContract ) );
                    }

                    if ( outputContractBlock != null )
                    {
                        statements.Add( new InsertedStatement( outputContractBlock, parameter, this, InsertedStatementKind.OutputContract ) );
                    }

                    return statements;
                }

            default:
                throw new AssertionFailedException( $"Unsupported contract target: {this.ContractTarget}" );
        }
    }

    public override IFullRef<IMember> TargetMember => this._targetIndexer;

    public override FormattableString ToDisplayString()
    {
        if ( this.ContractTarget.GetTarget( this.InitialCompilation ) is IIndexer )
        {
            return $"Add contract to indexer '{this._targetIndexer}'";
        }

        return base.ToDisplayString();
    }
}