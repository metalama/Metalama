// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Aspects;

/// <summary>
/// Regression test for https://github.com/metalama/Metalama/issues/1405.
/// Metalama should not interfere with partial classes where one part is source-generated
/// (e.g., by a third-party source generator like Catglobe.ResXFileCodeGenerator),
/// even when no aspects are applied.
/// </summary>
public sealed class Issue1405Tests : AspectTestBase
{
    /// <summary>
    /// Tests that the pipeline does not break a partial class whose other part provides
    /// a static property (simulating a source-generated partial class).
    /// The user's code calls the static property from the generated part.
    /// </summary>
    [Fact]
    public async Task PartialClassWithSourceGeneratedStaticMember_NoAspects()
    {
        using var testContext = this.CreateTestContext();

        // This simulates the scenario from issue #1405:
        // - SR.cs: user code that calls ResourceManager.GetString (a static member from the generated part)
        // - SR.Generated.cs: source-generated code that provides the static ResourceManager property
        var code = new Dictionary<string, string>
        {
            ["SR.cs"] = """
                        using System.Resources;

                        namespace System
                        {
                            internal static partial class SR
                            {
                                private static readonly bool _usingResourceKeys;

                                public static string? GetResourceString(string resourceKey)
                                {
                                    string? resourceString = ResourceManager.GetString(resourceKey);

                                    return resourceString;
                                }
                            }
                        }
                        """,
            ["SR.Generated.cs"] = """
                                  using System.Resources;
                                  using System.Reflection;

                                  namespace System
                                  {
                                      internal static partial class SR
                                      {
                                          private static ResourceManager? s_resourceManager;

                                          public static ResourceManager ResourceManager
                                          {
                                              get
                                              {
                                                  if (s_resourceManager == null)
                                                  {
                                                      s_resourceManager = new ResourceManager("SR", typeof(SR).Assembly);
                                                  }

                                                  return s_resourceManager;
                                              }
                                          }
                                      }
                                  }
                                  """
        };

        // The pipeline should succeed without errors, even though no aspects are applied.
        var result = await CompileAsync( testContext, code );

        Assert.True( result.IsSuccessful );
    }
}
