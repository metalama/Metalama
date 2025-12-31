# TemplatingCodeValidator Benchmark

Benchmarks the `TemplatingCodeValidator` against the entire nopCommerce solution (~4400 syntax trees across 35 projects).

## Prerequisites

### 1. Clone nopCommerce

Clone nopCommerce to a sibling directory of the Metalama repo:

```bash
cd C:\src\Metalama-2026.0  # or Metalama-2025.1
git clone https://github.com/nopSolutions/nopCommerce.git nopCommerce-benchmark
```

### 2. Fix SDK Version

The nopCommerce `global.json` requires SDK 9.0.205. If you have a different SDK version, update the rollForward policy:

Edit `nopCommerce-benchmark/src/global.json`:
```json
{
  "sdk": {
    "version": "9.0.205",
    "rollForward": "latestMajor"
  }
}
```

### 3. Restore nopCommerce

```bash
cd nopCommerce-benchmark/src
dotnet restore NopCommerce.sln
```

## Running the Benchmark

### Quick Test Mode

Verify the setup works without running full BenchmarkDotNet iterations:

```bash
dotnet run -c Release -- --test
```

### Quick Test with dotTrace Profiling

Run under dotTrace profiler with automatic snapshot saving:

```bash
dotnet run -c Release -- --test --dottrace
```

The snapshot will be saved with the branch name (e.g., `TemplatingCodeValidator-topic-2025.1-validator-benchmark`).

### Full Benchmark

Run the complete BenchmarkDotNet benchmark with multiple iterations:

```bash
dotnet run -c Release
```

Results are saved to `BenchmarkDotNet.Artifacts/results/`.

## Expected Results

- **Projects**: 35
- **Syntax Trees**: ~4389
- **Mean Time**: ~9-10 seconds per iteration (varies by hardware)
- **Memory**: ~12 GB allocated per iteration

## Configuration

The benchmark uses:
- `InProcessEmitToolchain` (avoids .NET SDK 10.0 `/p:` syntax issues)
- 30-minute timeout for long-running benchmarks
- 5% maximum relative error threshold
- Fresh `TestContext` per iteration to avoid caching effects
- `ConcurrentTaskRunner` for parallel validation

## Path Configuration

Set the `METALAMA_BENCHMARK_NOPCOMMERCE_SOLUTION` environment variable to the path of the nopCommerce solution file:

```bash
# Windows (PowerShell)
$env:METALAMA_BENCHMARK_NOPCOMMERCE_SOLUTION = "C:\src\Metalama-2026.0\nopCommerce-benchmark\src\NopCommerce.sln"

# Windows (cmd)
set METALAMA_BENCHMARK_NOPCOMMERCE_SOLUTION=C:\src\Metalama-2026.0\nopCommerce-benchmark\src\NopCommerce.sln
```

The benchmark will fail with a clear error if this environment variable is not set or the file doesn't exist.
