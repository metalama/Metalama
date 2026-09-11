// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @LanguageVersion(15.0)
#endif

// The getter of an observable property contains a labeled loop, a labeled break and a labeled continue. The dependency
// walker of the Observability aspect reaches the label through the paths it already has: a labeled statement is visited
// like any other statement, and the identifier of a labeled jump binds to a label symbol, which the classification of a
// reference chain marks unsupported instead of reporting it. The dependency on Threshold and on Count is still found.
// No case of the walker is added for these constructs. See issue #1947.
//
// The project compiles its own test sources at the language version of LangMaxVersion, which is 14.0 and does not
// accept a labeled break. The project file therefore removes this file from the compilation. The test framework reads
// the file from the source directory, so the test still runs.

namespace Metalama.Patterns.Observability.AspectTests.LabeledLoopInGetter;

[Observable]
public class ViewModel
{
    public int Threshold { get; set; }

    public int Count { get; set; }

    public int FirstValueOverThreshold
    {
        get
        {
            var result = 0;

        outer:

            for ( var i = 0; i < this.Count; i++ )
            {
                for ( var j = i; j < this.Count; j++ )
                {
                    if ( j <= this.Threshold )
                    {
                        continue outer;
                    }

                    result = j;

                    if ( result > this.Threshold )
                    {
                        break outer;
                    }
                }
            }

            return result;
        }
    }
}
