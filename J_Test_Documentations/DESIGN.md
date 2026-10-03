# J-Test — Baseline Design

## 1. Design Objective
J-Test is designed to reduce the manual effort required to derive functional test cases from software user stories. A user can enter a story directly or retrieve stories from Jira, generate test cases through Gemini, review the generated output, and export it.

This document records the design of the supplied baseline code before re-engineering.

## 2. Major Modules

### 2.1 Authentication and Sign-up
Frontend:
- `src/Components/Login.js`
- `src/Components/Signup.js`

Backend:
- `Controllers/AuthController.cs`

Data:
- `Models/Credential.cs`
- `Models/JTestCredentialsContext.cs`

The sign-up flow creates a `Credential` record. The login flow queries by email and validates the supplied password against the stored credential.

### 2.2 Jira Credential Management
Frontend:
- `src/Components/Settings.js`

Backend:
- `AuthController`

Data:
- `Detail`

Users can enter:
- Jira domain
- Jira username
- Jira API token

The backend persists these values. Stored entries can be retrieved, selected, and deleted.

### 2.3 Jira User Story Import
Frontend:
- `src/Components/ImportDataSidebar.js`

Backend:
- `UserStoryDescriptionController.ExportAllUserStories`

The frontend supplies domain, project, issue type, username, and API token. The backend builds a Jira search request, authenticates to Jira, retrieves issues, and maps selected issue fields into `IssueDetail`.

### 2.4 Functional Test Case Generation
Frontend:
- `src/Components/ChatBox.js`

Backend:
- `UserStoryDescriptionController.GenerateTestCases`

The frontend submits user-story text. The backend performs simple textual validation and calls Gemini with a prompt requesting functional test cases containing pass and fail scenarios.

The Gemini response is returned to the frontend, where generated Markdown content is converted to HTML for display.

### 2.5 Export
Frontend:
- `src/Components/ChatBox.js`

Generated bot messages are converted to an HTML table and passed to `html-docx-js` to produce a `.docx` download.

## 3. Request Flow — Generate Test Cases

```text
User
 |
 | enters/selects user story
 v
ChatBox.js
 |
 | POST /api/UserStoryDescription/GenerateTestCases
 | { userStory: "..." }
 v
UserStoryDescriptionController
 |
 | Validate input
 |
 | POST Gemini generateContent
 v
Gemini
 |
 | JSON response
 v
Controller
 |
 | HTTP 200
 v
ChatBox.js
 |
 | Extract candidate content
 | Convert Markdown using marked()
 v
Rendered test cases
```

## 4. Request Flow — Import Jira Stories

```text
User selects Jira credential
        |
        v
Settings.js
        |
        | React Router state
        v
ChatBox.js
        |
        v
ImportDataSidebar.js
        |
        | domain/project/issueType/
        | username/apiToken
        v
GET ExportAllUserStories
        |
        v
UserStoryDescriptionController
        |
        | Basic authentication
        v
Jira REST API
        |
        v
IssueDetail list
        |
        v
ImportDataSidebar
```

## 5. REST Design

### Authentication / Credential APIs

| Method | Route | Purpose |
|---|---|---|
| POST | `/api/Auth/signup` | Register an application user |
| POST | `/api/Auth/login` | Validate application credentials |
| GET | `/api/Auth/details` | Retrieve saved Jira connection details |
| GET | `/api/Auth/credentials` | Retrieve stored application credentials |
| POST | `/api/Auth/save` | Save Jira connection details |
| DELETE | `/api/Auth/detail/delete/{id}` | Delete a Jira detail record |
| DELETE | `/api/Auth/credential/delete/{id}` | Delete an application credential |

### User Story APIs

| Method | Route | Purpose |
|---|---|---|
| POST | `/api/UserStoryDescription/GenerateTestCases` | Generate functional test cases from a user story |
| GET | `/api/UserStoryDescription/ExportAllUserStories` | Retrieve Jira user stories |

## 6. Data Model

```text
Credential
----------
id : int (PK)
email : string
password : string


Detail
------
Id : int (PK)
domain : string
username : string
apiToken : string
```

The two entities are independent in the supplied model; no explicit foreign-key relationship between an application user and saved Jira details is defined in `JTestCredentialsContext`.

## 7. Validation Design

### Signup
The backend verifies:
- request is not null
- email is populated
- password is populated
- email does not already exist

### Login
The backend verifies:
- request is not null
- email is populated
- password is populated
- user exists
- supplied password matches stored password

### Jira Detail Save
The backend verifies:
- request is not null
- domain is populated
- username is populated
- API token is populated

### User Story
The backend checks for several expected user-story phrases, including:
- `As a`
- `I want to`
- `I want an`
- `so that`
- `The user`
- `I want a`

### Jira Import
The backend requires:
- domain
- project
- issue type
- username
- API token

## 8. Error Handling
Controllers return HTTP status responses such as:
- 200 OK
- 400 Bad Request
- 401 Unauthorized
- 404 Not Found
- 500 Internal Server Error

The application also uses `ILogger` in `UserStoryDescriptionController` for informational, warning, and error logging.

## 9. UI State Design
The React application uses component-level state through hooks such as:
- `useState`
- `useEffect`
- `useLocation`
- `useNavigate`

Selected Jira credential information is passed through React Router navigation state into the test-generation workflow.

## 10. Current Design Constraints
The baseline implementation is a PoC-oriented design:
- frontend API endpoints are tied to a local backend URL
- authentication is implemented as controller-level credential validation rather than a complete authenticated session/token architecture
- application credentials and Jira details use simple persistence models
- external API interactions are implemented directly inside the controller
- a static `HttpClient` is shared by `UserStoryDescriptionController`
- external-service and persistence concerns are not separated into dedicated service/repository layers

These are observations of the existing design, not remediation decisions. Security-specific findings will be handled in the subsequent assessment.
