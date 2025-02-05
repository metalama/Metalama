// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Aspects;

internal static class ContractAspectHelper
{
    internal static ContractDirection GetEffectiveDirection( ContractDirection direction, IDeclaration targetDeclaration )
    {
        if ( direction == ContractDirection.Default )
        {
            return targetDeclaration switch
            {
                IParameter { IsReturnParameter: true } => ContractDirection.Output,
                IParameter { RefKind: RefKind.Out } => ContractDirection.Output,
                IParameter => ContractDirection.Input,
                IFieldOrPropertyOrIndexer { SetMethod: null } => ContractDirection.Output,
                IFieldOrPropertyOrIndexer => ContractDirection.Input,
                _ => throw new ArgumentOutOfRangeException( nameof(targetDeclaration), $"Unexpected kind of declaration: '{targetDeclaration}'." )
            };
        }
        else
        {
            return direction;
        }
    }

    internal static ContractDirection CombineWith( this ContractDirection a, ContractDirection b )
        => (a, b) switch
        {
            (ContractDirection.None, _) => b,
            (_, ContractDirection.None) => a,
            (ContractDirection.Both, _) => ContractDirection.Both,
            (_, ContractDirection.Both) => ContractDirection.Both,
            (ContractDirection.Input, ContractDirection.Output) => ContractDirection.Both,
            (ContractDirection.Output, ContractDirection.Input) => ContractDirection.Both,
            (ContractDirection.Default, _) => b,
            (_, ContractDirection.Default) => a,
            _ => throw new ArgumentOutOfRangeException( $"Unexpected combination: ({a}, {b})" )
        };

    internal static ContractDirection GetPossibleDirection( IDeclaration targetDeclaration )
        => targetDeclaration switch
        {
            IParameter { IsReturnParameter: true } => ContractDirection.Output,
            IParameter { RefKind: RefKind.Out } => ContractDirection.Output,
            IParameter { RefKind: RefKind.Ref } => ContractDirection.Both,
            IParameter => ContractDirection.Input,
            IFieldOrPropertyOrIndexer { SetMethod: null } => ContractDirection.Output,
            IFieldOrPropertyOrIndexer { GetMethod: null } => ContractDirection.Input,
            IFieldOrPropertyOrIndexer => ContractDirection.Both,
            _ => throw new ArgumentOutOfRangeException( nameof(targetDeclaration), $"Unexpected kind of declaration: '{targetDeclaration}'." )
        };

    internal static ContractDirection Restrict( this ContractDirection a, ContractDirection b )
        => (a, b) switch
        {
            (ContractDirection.None, _) => ContractDirection.None,
            (_, ContractDirection.None) => ContractDirection.None,
            (ContractDirection.Both, _) => b,
            (_, ContractDirection.Both) => a,
            (ContractDirection.Input, ContractDirection.Input) => ContractDirection.Input,
            (ContractDirection.Output, ContractDirection.Output) => ContractDirection.Output,
            (ContractDirection.Input, ContractDirection.Output) => ContractDirection.None,
            (ContractDirection.Output, ContractDirection.Input) => ContractDirection.None,
            _ => throw new ArgumentOutOfRangeException( $"Unexpected combination: ({a}, {b})" )
        };
}