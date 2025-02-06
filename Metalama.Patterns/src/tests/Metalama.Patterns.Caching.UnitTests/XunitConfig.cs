// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Xunit;

/*
 * Configuration of the Xunit behaviors, such as tests parallelization.
 * See https://xunit.net/docs/running-tests-in-parallel
 */

[assembly: CollectionBehavior( DisableTestParallelization = true )]