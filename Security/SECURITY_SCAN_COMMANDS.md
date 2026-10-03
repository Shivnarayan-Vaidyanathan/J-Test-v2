# J-Test — Baseline Security Scan Commands

Run these commands from your local development machine **before remediation** and save the output. These scans complete the dependency portion of Step 2.

## 1. Frontend Dependency Scan

**Exact location:**

```text
J-Test/j-test-ui/
```

Run:

```powershell
npm ci
npm audit
npm audit --json > ..\security\npm-audit-baseline.json
```

Do **not** run `npm audit fix` yet. Step 2 is evidence collection only.

## 2. Backend Dependency Scan

**Exact location:**

```text
J-Test/J-Test/
```

Run:

```powershell
dotnet restore
dotnet list J-Test.csproj package --vulnerable --include-transitive
dotnet list J-Test.csproj package --outdated --include-transitive
```

Save the output:

```powershell
dotnet list J-Test.csproj package --vulnerable --include-transitive > ..\security\dotnet-vulnerable-baseline.txt
dotnet list J-Test.csproj package --outdated --include-transitive > ..\security\dotnet-outdated-baseline.txt
```

## 3. Secret Search

From the repository root:

```powershell
git grep -n -i -E "api.?key|secret|password|token|authorization"
```

Also inspect Git history for the exposed Gemini key/secret-bearing changes. If an active credential has been committed, rotate/revoke it rather than merely deleting it from the latest source.

## 4. Build Baseline

Backend:

```powershell
cd J-Test
dotnet restore
dotnet build
```

Frontend:

```powershell
cd ..\j-test-ui
npm ci
npm test -- --watchAll=false
npm run build
```

Capture these outputs before remediation. They establish whether any later failure was introduced by the security changes.

## 5. Store Evidence Here

```text
J-Test/
└── security/
    ├── BASELINE_SECURITY_REPORT.md
    ├── SECURITY_SCAN_COMMANDS.md
    ├── npm-audit-baseline.json
    ├── dotnet-vulnerable-baseline.txt
    ├── dotnet-outdated-baseline.txt
    └── baseline-build-test-results.txt
```

The generated JSON/text outputs should reflect the actual local scan; do not hand-edit scanner output.
