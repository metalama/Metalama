// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;

[assembly: AspectOrder(
    AspectOrderDirection.RunTime,
    "Metalama.Framework.Aspects.ContractAspect",
    "Metalama.Patterns.Observability.ObservableAttribute" )]