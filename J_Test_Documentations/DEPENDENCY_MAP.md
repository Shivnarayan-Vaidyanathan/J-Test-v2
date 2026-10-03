# J-Test — Baseline Dependency Map

## 1. Purpose
This document records the principal application, framework, library, database, and external-service dependencies visible in the supplied J-Test baseline.

It is a dependency inventory/map, not yet a vulnerability report. Security and version findings will be established during the dedicated security scan.

## 2. System Dependency Map

```text
J-Test
|
+-- React Frontend
|   |
|   +-- React / React DOM
|   +-- React Router
|   +-- Material UI
|   +-- Emotion
|   +-- marked
|   +-- html-docx-js
|   +-- react-scripts
|   +-- Testing Library
|   |
|   +------ REST ------> ASP.NET Core Backend
|
+-- ASP.NET Core Backend (.NET 8)
    |
    +-- Entity Framework / EF Core
    |       |
    |       +------> SQL Server / LocalDB
    |
    +-- Swashbuckle / OpenAPI
    +-- Newtonsoft.Json (used by controller source)
    +-- Google Cloud AI Platform package
    +-- Jira.SDK
    +-- IdentityModel
    |
    +------ HTTPS ------> Google Gemini API
    |
    +------ HTTPS ------> Jira REST API
```

## 3. Frontend Manifest
**Source:** `J-Test/j-test-ui/package.json`

Declared dependencies include:

| Package | Declared Version | Role |
|---|---:|---|
| `@emotion/react` | `^11.14.0` | CSS-in-JS / MUI styling |
| `@emotion/styled` | `^11.14.0` | Styled components for MUI |
| `@mui/icons-material` | `^6.3.0` | Material icons |
| `@mui/material` | `^6.3.0` | UI component library |
| `@testing-library/jest-dom` | `^5.17.0` | DOM test assertions |
| `@testing-library/react` | `^13.4.0` | React component testing |
| `@testing-library/user-event` | `^13.5.0` | User-event simulation |
| `cors` | `^2.8.5` | CORS package declared in frontend manifest |
| `react` | `^18.3.1` | UI framework |
| `react-dom` | `^18.3.1` | React DOM renderer |
| `react-router-dom` | `^7.0.2` | Client-side routing |
| `react-scripts` | `5.0.1` | Create React App build/test tooling |
| `web-vitals` | `^2.1.4` | Browser performance metrics |

### Additional frontend imports visible in source
`ChatBox.js` imports:
- `marked`
- `html-docx-js/dist/html-docx`

These should be reconciled against the lockfile/manifests during the dependency-security stage because they are directly used by runtime source.

## 4. Backend Manifest
**Source:** `J-Test/J-Test/J-Test.csproj`

Target framework:
- `.NET 8` / `net8.0`

Declared NuGet dependencies:

| Package | Declared Version | Role |
|---|---:|---|
| `EntityFramework` | `6.5.1` | Entity Framework package |
| `Google.Cloud.AIPlatform.V1` | `3.13.0` | Google AI Platform client library |
| `Jira.SDK` | `1.2.25` | Jira integration package |
| `Microsoft.AspNetCore.Cors` | `2.1.1` | ASP.NET Core CORS package |
| `Microsoft.EntityFrameworkCore` | `9.0.0` | EF Core |
| `Microsoft.EntityFrameworkCore.Design` | `9.0.0` | EF design-time tooling |
| `Microsoft.EntityFrameworkCore.SqlServer` | `9.0.0` | SQL Server provider |
| `Microsoft.EntityFrameworkCore.Tools` | `9.0.0` | EF tooling |
| `Microsoft.IdentityModel.Tokens` | `8.3.0` | Token/security primitives |
| `Microsoft.OpenApi` | `1.6.22` | OpenAPI model |
| `Microsoft.VisualStudio.Azure.Containers.Tools.Targets` | `1.21.0` | Container tooling |
| `Swashbuckle.AspNetCore` | `7.2.0` | Swagger/OpenAPI |
| `Swashbuckle.AspNetCore.Annotations` | `7.2.0` | Swagger annotations |

The source also imports `Newtonsoft.Json`; resolved package/transitive dependency details are present in generated NuGet asset files and should be validated during the dependency scan.

## 5. Database Dependencies

### Database engine
SQL Server / LocalDB.

### Configuration source
`J-Test/J-Test/appsettings.json`

### EF Core context
`J-Test/J-Test/Models/JTestCredentialsContext.cs`

### Entities
- `Credential`
- `Detail`

### Supplied database artifacts
The repository contains:
- `Database/J_Test_Credentials.mdf`
- `Database/J_Test_Credentials_log.ldf`

## 6. External Runtime Dependencies

### Google Gemini API
Used by:
`J-Test/J-Test/Controllers/UserStoryDescription.cs`

Purpose:
Generate functional test cases from submitted user-story text.

### Jira REST API
Used by:
`J-Test/J-Test/Controllers/UserStoryDescription.cs`

Purpose:
Search Jira issues and retrieve user-story information.

Authentication in the baseline request is based on Jira username plus API token.

## 7. Internal Component Dependencies

```text
App.js
 |
 +--> Login.js ---------> AuthController.login
 |
 +--> Signup.js --------> AuthController.signup
 |
 +--> Settings.js ------> AuthController
 |                        |-- details
 |                        |-- credentials
 |                        |-- save
 |                        |-- delete
 |
 +--> ChatBox.js --------> UserStoryDescriptionController
      |                    |-- GenerateTestCases
      |
      +--> ImportDataSidebar.js
                           |
                           +--> ExportAllUserStories
```

Backend persistence relationship:

```text
AuthController
      |
      v
JTestCredentialsContext
      |
      +--> Credentials --> Credential
      |
      +--> Details -----> Detail
```

Backend external-service relationship:

```text
UserStoryDescriptionController
      |
      +--> Gemini Generate Content API
      |
      +--> Jira REST API
```

## 8. Build and Development Dependencies

### Frontend
Primary scripts:
- `npm start`
- `npm run build`
- `npm test`
- `npm run eject`

### Backend
The solution/project files support standard .NET build/run workflows and include Docker development support.

## 9. Dependency Assessment Items for the Next Stage
The following are intentionally deferred to the security-assessment phase:
- known CVEs in direct dependencies
- known CVEs in transitive dependencies
- unsupported/EOL package versions
- incompatible framework/package combinations
- unnecessary dependencies
- secret exposure
- package integrity
- frontend `npm audit` findings
- backend NuGet vulnerability findings

This separation preserves the baseline documentation before remediation.
