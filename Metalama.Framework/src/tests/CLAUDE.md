# Running the test suites

## Never truncate the output of a test run

Redirect the whole output of `dotnet test` to a file and inspect the file:

```powershell
dotnet test <project> -c Debug -f net8.0 --no-build > run.log 2>&1
```

Then read `run.log`: the summary line is at the end, and the names of the failing tests are the lines
beginning with `  Failed `.

**Do not pipe the run through `tail`, `head`, or a `grep` that keeps only the summary.** The summary line
survives such a filter and reports how many tests failed, but the names of the failing tests do not, so the
only way to learn what failed is to run the suite again. That mistake cost a second run of the aspect suite,
which takes over eight minutes.

The same reasoning applies to a filter that keeps only the lines matching an expected failure pattern: it
reports nothing when the run fails in an unexpected way, and silence then reads as success.

This is the rule that `CLAUDE.md` states for builds, applied to test runs, where it is more expensive because
a suite takes minutes rather than seconds.

## Which suite sees what

The unit tests and the aspect tests do not overlap, and a change to the code model can pass one and fail the
other:

- `Metalama.Framework.Tests.UnitTests` exercises the code model directly. It runs in about a minute.
- `Metalama.Framework.Tests.AspectTests` compares generated code against expected output, so it is the only
  suite that sees a change in what the pipeline emits. It runs in about eight minutes.

A change to comparison, conversion, nullability or the representation of a type has to be run against the
aspect tests before it can be trusted, however green the unit tests are.

## Related documentation

The test strategies and every suite are documented in `Metalama.Framework/docs/testing.md`. Read it before
writing or debugging a test.
