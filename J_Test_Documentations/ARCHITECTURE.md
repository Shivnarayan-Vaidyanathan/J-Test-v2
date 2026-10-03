# J-Test — Baseline Architecture

## 1. Purpose
J-Test is a full-stack Functional Test Case Generator. It accepts manually entered user stories or imports user stories from Jira, sends the selected user story to Google's Gemini API to generate functional test cases, displays the generated result in a React UI, and supports export of generated content to a Word document.

This document describes the application as supplied in the baseline ZIP. It intentionally documents the current implementation before security remediation.

## 2. Technology Stack

### Frontend
- React 18.3.1
- React DOM 18.3.1
- React Router DOM 7.0.2
- Material UI / MUI
- `marked` for Markdown-to-HTML rendering (used in source)
- `html-docx-js` for Word export (used in source)
- Create React App / `react-scripts` 5.0.1

### Backend
- ASP.NET Core Web API
- Target framework: .NET 8 (`net8.0`)
- Entity Framework Core
- SQL Server / LocalDB
- Swagger / Swashbuckle
- Newtonsoft.Json
- HTTP integrations with Gemini and Jira

### Data Storage
The backend uses `JTestCredentialsContext` and two logical entities:
- `Credential` — application sign-up/login data
- `Detail` — Jira domain, username, and API token data

The supplied repository also contains SQL Server database files under `Database/`.

## 3. High-Level Architecture

```text
+-----------------------------+
|          End User           |
+--------------+--------------+
               |
               | Browser
               v
+-----------------------------+
|      React Frontend         |
|       j-test-ui             |
|                             |
| Login / Signup              |
| Settings                    |
| Chat / Test Generator       |
| Jira Import Sidebar         |
| Word Export                 |
+--------------+--------------+
               |
               | HTTPS / REST
               | localhost:7216
               v
+-----------------------------+
|    ASP.NET Core Web API     |
|                             |
| AuthController              |
| UserStoryDescription        |
| Controller                  |
+------+----------------+-----+
       |                |
       | EF Core        | HTTP
       v                +--------------------+
+--------------+        |                    |
| SQL Server / |        v                    v
| LocalDB      |  +------------+      +-------------+
|              |  | Gemini API |      |  Jira API   |
| Credentials  |  | Test-case  |      | User-story  |
| Details      |  | generation |      | retrieval   |
+--------------+  +------------+      +-------------+
```

## 4. Frontend Architecture

The frontend is located at:

`J-Test/j-test-ui/`

Application code is primarily under:

`J-Test/j-test-ui/src/`

Key components include:

| Component | Responsibility |
|---|---|
| `App.js` | Application-level routing |
| `Login.js` | Login UI and login API interaction |
| `Signup.js` | User registration UI |
| `HomePage.js` | Landing page after navigation |
| `Settings.js` | Jira credential management and stored sign-up display |
| `ChatBox.js` | User-story entry, Gemini test generation display, export |
| `ImportDataSidebar.js` | Jira parameters and Jira user-story retrieval |
| `SideNav.js` | Application navigation |

The frontend calls the backend directly using `fetch`, with backend URLs currently embedded in component source.

## 5. Backend Architecture

Backend location:

`J-Test/J-Test/`

### Application Bootstrap
`Program.cs` configures:
- ASP.NET Core controllers
- Swagger/OpenAPI
- CORS
- `JTestCredentialsContext`
- SQL Server database connectivity
- application request pipeline

### Controllers

#### `AuthController`
Location:

`J-Test/J-Test/Controllers/AuthController.cs`

Responsibilities:
- User sign-up
- User login
- Retrieve stored Jira details
- Retrieve application credentials
- Save Jira connection details
- Delete Jira details
- Delete application credentials

#### `UserStoryDescriptionController`
Location:

`J-Test/J-Test/Controllers/UserStoryDescription.cs`

Responsibilities:
- Validate submitted user-story text
- Call Gemini to generate functional test cases
- Call Jira REST API to retrieve user stories
- Convert Jira issue responses into application models

## 6. Data Layer

### `JTestCredentialsContext`
Location:

`J-Test/J-Test/Models/JTestCredentialsContext.cs`

Entity Framework Core context exposing:
- `DbSet<Credential> Credentials`
- `DbSet<Detail> Details`

### Credential
Fields:
- `id`
- `email`
- `password`

### Detail
Fields:
- `Id`
- `domain`
- `username`
- `apiToken`

## 7. External Integrations

### Google Gemini
The backend sends user-story content to the Gemini Generate Content endpoint and returns the model response to the frontend.

Flow:

```text
ChatBox
   |
   | POST GenerateTestCases
   v
UserStoryDescriptionController
   |
   | Gemini request
   v
Gemini API
   |
   | generated test cases
   v
Backend -> React UI
```

### Jira
Jira integration retrieves issues based on:
- Jira domain
- project
- issue type
- username
- API token

Flow:

```text
Settings / selected Jira credential
             |
             v
ImportDataSidebar
             |
             | ExportAllUserStories
             v
UserStoryDescriptionController
             |
             | Jira REST API
             v
           Jira
             |
             v
IssueDetail[] -> React sidebar
```

## 8. Deployment / Runtime Characteristics
- Backend contains a Dockerfile.
- Frontend contains a Dockerfile.
- Backend development configuration uses HTTPS.
- Frontend API calls target `https://localhost:7216`.
- Backend database configuration currently targets SQL Server LocalDB.
- Swagger is enabled in development.

## 9. Architectural Boundaries

The solution can be divided into four logical boundaries:

1. **Presentation** — React components and browser-side state.
2. **Application/API** — ASP.NET Core controllers.
3. **Persistence** — Entity Framework Core and SQL Server.
4. **External services** — Gemini and Jira.

## 10. Baseline Note
This is a baseline architecture description only. Security weaknesses visible in the architecture or source are intentionally not remediated here. They will be recorded and classified separately during the security-assessment stage.
