// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;

[assembly: AspectOrder(
    AspectOrderDirection.CompileTime,
    "Metalama.Patterns.Observability.ObservableAttribute",
    "Metalama.Patterns.Wpf.CommandAttribute" )]

[assembly:
    AspectOrder(
        AspectOrderDirection.CompileTime,
        "Metalama.Patterns.Contracts.ContractBaseAttribute",
        "Metalama.Patterns.Wpf.DependencyPropertyAttribute",
        "Metalama.Patterns.Contracts.ContractAspect:" + ContractAspect.BuildLayer )]