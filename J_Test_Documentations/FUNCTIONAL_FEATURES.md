# J-Test — Baseline Functional Features

## 1. Application Purpose
J-Test is a Functional Test Case Generator that combines a React frontend, an ASP.NET Core backend, Jira integration, and Gemini-based generation.

## 2. User Registration
**Frontend location:** `J-Test/j-test-ui/src/Components/Signup.js`

**Backend location:** `J-Test/J-Test/Controllers/AuthController.cs`

The application allows a user to create an account using an email address and password.

Baseline behavior:
- validates that required values are supplied
- checks whether the email already exists
- creates a new credential record
- returns a success response after persistence

## 3. User Login
**Frontend location:** `J-Test/j-test-ui/src/Components/Login.js`

**Backend location:** `J-Test/J-Test/Controllers/AuthController.cs`

The application accepts an email/password pair and checks it against stored credentials.

Baseline outcomes include:
- invalid request
- user not found
- invalid password
- successful login

## 4. Jira Connection Settings
**Frontend location:** `J-Test/j-test-ui/src/Components/Settings.js`

Users can maintain Jira connection information consisting of:
- domain
- Jira username
- Jira API token

Functions include:
- save Jira details
- retrieve stored Jira details
- select one stored Jira detail
- delete a Jira detail

## 5. Stored Sign-up Management
**Frontend location:** `J-Test/j-test-ui/src/Components/Settings.js`

The Settings screen also retrieves stored application credentials and displays the email together with a masked password representation.

A selected credential record can be deleted.

## 6. Jira User Story Import
**Frontend location:** `J-Test/j-test-ui/src/Components/ImportDataSidebar.js`

**Backend location:** `J-Test/J-Test/Controllers/UserStoryDescription.cs`

The Jira import sidebar collects:
- domain
- project
- issue type
- username
- API token

It invokes the backend to retrieve matching Jira issues. Returned stories display:
- summary
- reporter
- description

Selecting a story copies its description into the test-case-generation input.

## 7. Manual User Story Entry
**Frontend location:** `J-Test/j-test-ui/src/Components/ChatBox.js`

Users can manually type a user story into the chat-style interface instead of importing one from Jira.

## 8. User Story Validation
**Backend location:** `J-Test/J-Test/Controllers/UserStoryDescription.cs`

Before generation, the backend applies a simple phrase-based user-story validation rule.

Empty stories are rejected.

## 9. AI Functional Test Case Generation
**Frontend location:** `J-Test/j-test-ui/src/Components/ChatBox.js`

**Backend location:** `J-Test/J-Test/Controllers/UserStoryDescription.cs`

The backend sends the user story to Gemini with instructions to generate functional test cases containing:
- pass scenarios
- fail scenarios
- test case description
- expected result
- actual result

The frontend extracts generated candidate content and displays it in the chat interface.

## 10. Greeting Handling
**Frontend location:** `J-Test/j-test-ui/src/Components/ChatBox.js`

Simple greetings such as `hi`, `hello`, and `hey` are handled locally without calling the generation API.

## 11. Markdown Rendering
**Frontend location:** `J-Test/j-test-ui/src/Components/ChatBox.js`

Generated Markdown is converted to HTML using `marked` and displayed in the chat interface.

## 12. Word Export
**Frontend location:** `J-Test/j-test-ui/src/Components/ChatBox.js`

Generated bot messages can be exported to a Word document.

The UI:
1. selects bot responses
2. builds an HTML table
3. converts the HTML to a DOCX blob
4. initiates a browser download named `Functional Test Cases.docx`

## 13. Navigation
The React UI provides navigation between application screens using React Router and `SideNav`.

Principal screens/components include:
- Login
- Signup
- Home
- Settings
- Chat/Test Case Generator

## 14. API Summary

| Capability | Backend API |
|---|---|
| Sign up | `POST /api/Auth/signup` |
| Login | `POST /api/Auth/login` |
| Read Jira details | `GET /api/Auth/details` |
| Read application credentials | `GET /api/Auth/credentials` |
| Save Jira details | `POST /api/Auth/save` |
| Delete Jira detail | `DELETE /api/Auth/detail/delete/{id}` |
| Delete application credential | `DELETE /api/Auth/credential/delete/{id}` |
| Generate test cases | `POST /api/UserStoryDescription/GenerateTestCases` |
| Import Jira stories | `GET /api/UserStoryDescription/ExportAllUserStories` |

## 15. Baseline Scope Boundary
The repository represents a PoC. This feature inventory describes behavior visible in the supplied source and existing documentation. It does not claim that each function is production-hardened or secured. Security assessment and remediation are separate re-engineering stages.
