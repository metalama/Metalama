## When writing XML documentation

- Use `<see>` tags where possible.
- Use consistent lexicon, style and structure among classes and files that have the same suffix (belong to the same family).
- Do not write long code examples.
- Read the related conceptual documentation in project `../Metalama.Documentation/content` mentioned by DocFx uid in the `<seealso href="@..."/>` tags.
- Complete the API doc with the conceptual documentation where relevant.

## Pre-PR checks and enhancements

- Documentation
    - Check and complete the documentation of all new or modified APIs.
    - Look for relevant conceptual articles in `../Metalama.Documentation/content` using keyword search.
    - Suggest changes in affected conceptual articles

## Aspect tests

- In aspect tests, Foo.t.cs is the result file of Foo.cs

## Git branches

- Branch naming convention: `topic/YYYY.N/XXXX-short-description` where `XXXX` is the issue number
- For a branch named `topic/YYYY.N/*`, the merge branch is always `develop/YYYY.N` - do not use the default merge branch

## Commits

- Commit messages must include the issue number, e.g. `(#1212)`
- Do not sign commits with "Generated with Claude Code"