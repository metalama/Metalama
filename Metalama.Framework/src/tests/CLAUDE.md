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

## An expected output is not the truth

When a change makes an aspect test produce a different output, decide which of the two outputs is correct
before deciding what to fix. The expected file records what the code did when the file was written, which
may have been wrong. If the new output is the more correct one, the fix is to accept it as the new expected
output, not to change the product until the old output comes back.

A case from this repository. Recording the names of the elements of a tuple on its symbol changed the output
of twenty seven aspect tests:

```
expected: (object? , EventArgs)
actual:   (object? sender, EventArgs e)
```

The expected file was the wrong one. The stray space before the comma is where the name had been dropped,
and the aspect had asked for a named tuple: it built the type from the parameters of a delegate, which pairs
each type with the name of the parameter. The new output is what was asked for, and the old output recorded
the defect.

Two checks before accepting an output:

- **An output that differs is not necessarily an output that is wrong, and an output that is wrong is not
  necessarily a difference in the transformed code.** In that same run, one of the twenty seven failures
  began with `// CompileTimeAspectPipeline.ExecuteAsync`, which is the marker of a pipeline error rather
  than a difference of formatting. That one was a real regression and had to be diagnosed, not accepted.
- **Read each output rather than accepting the batch.** Tests that fail together usually fail for one
  reason, but scan the differences for any that is not the change being made.

The assertion is a string comparison against the transformed code, so a test that fails with
`Assert.Equal() Failure: Strings differ` has produced code that compiles. A failure to compile or to run
looks different, and is never something to accept.

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
