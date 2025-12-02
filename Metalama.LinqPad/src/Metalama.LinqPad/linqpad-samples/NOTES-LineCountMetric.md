# Line-of-Code Counting Metric - Design Notes

## Problem
Count lines of code (excluding blank lines and comments) for a syntax node using Roslyn's SyntaxTree.

## Key Insight
- Tokens exclude comments (comments are trivia in Roslyn)
- Multi-line tokens (verbatim strings, raw strings) span multiple lines but are a single token

## Naive Approach (Wrong)
```csharp
// Just count unique line numbers from tokens - WRONG
// Doesn't handle multi-line tokens correctly
foreach (var token in node.DescendantTokens())
{
    var line = token.GetLocation().GetLineSpan().StartLinePosition.Line;
    // This misses lines inside multi-line strings
}
```

## HashSet Approach (Correct but Expensive)
```csharp
var lines = new HashSet<int>();
foreach (var token in node.DescendantTokens())
{
    var span = token.GetLocation().GetLineSpan();
    for (int line = span.StartLinePosition.Line; line <= span.EndLinePosition.Line; line++)
    {
        lines.Add(line);
    }
}
return lines.Count;
```
- **Problem**: Maintaining a HashSet has O(n) space and overhead per insertion

## Efficient O(1) Space Approach (Recommended)
```csharp
var maxLineSeen = -1;
var count = 0;

foreach (var token in node.DescendantTokens())
{
    var span = token.GetLocation().GetLineSpan();
    var startLine = span.StartLinePosition.Line;
    var endLine = span.EndLinePosition.Line;

    if (endLine > maxLineSeen)
    {
        // Only count lines we haven't seen yet
        count += endLine - Math.Max(maxLineSeen, startLine - 1);
        maxLineSeen = endLine;
    }
}

return count;
```

### Why This Works
- Tokens from `DescendantTokens()` are returned in document order
- We only need to track the highest line number seen so far
- For each token, we add the lines that are beyond what we've already counted
- O(1) space, O(n) time where n is token count

### Edge Cases to Consider
- Empty nodes (return 0)
- Single-line nodes
- Nodes with only whitespace/comments (no tokens)
- Multi-line string literals spanning many lines
- Interpolated strings with embedded expressions
- /// XML comments with <see cref> tags (structured trivia)
- preprocessor directives
- /* */ single-line
- /* */ multi-line
- // comments

## TODO
- Implement the metric
- Implement unit tests for all edge cases
- Document
- Add to LINQPad samples
- Add to Metalama.Documentation