# J-Test — Baseline Security Assessment

## 1. Assessment Status

**Assessment stage:** Baseline / pre-remediation  
**Application:** J-Test  
**Scope reviewed:** React frontend, ASP.NET Core backend, configuration, persistence models, external-service integrations, and declared dependencies.

No remediation has been applied as part of this report. The purpose is to preserve the vulnerable baseline before the reusable remediation skill is created and used.

> Dependency-scanner note: an `npm audit` attempt could not reach the npm registry from the analysis environment, and the .NET SDK is not installed in that environment. Therefore package-CVE closure must be supplemented by running the commands in `SECURITY_SCAN_COMMANDS.md` on the development machine. Source/configuration findings below were verified directly from the supplied repository.

## 2. Executive Summary

The baseline contains multiple security weaknesses requiring remediation before the application should be treated as production-ready.

### Severity Summary

| ID | Finding | Severity | CWE | OWASP Top 10 |
|---|---|---|---|---|
| SEC-001 | Hard-coded Gemini API key in source | Critical | CWE-798 | A02:2021 Cryptographic Failures |
| SEC-002 | Application passwords stored and compared in plaintext | Critical | CWE-256 / CWE-916 | A02:2021 Cryptographic Failures |
| SEC-003 | Sensitive credential APIs lack effective authentication/authorization | High | CWE-306 / CWE-862 | A01:2021 Broken Access Control |
| SEC-004 | Credential-list endpoint returns password-bearing entities | High | CWE-200 | A01:2021 Broken Access Control |
| SEC-005 | Jira API tokens stored as plaintext and returned by API | High | CWE-312 / CWE-200 | A02:2021 Cryptographic Failures |
| SEC-006 | Jira API token is sent in URL query string | High | CWE-598 | A02:2021 Cryptographic Failures |
| SEC-007 | Jira Basic authorization credential is written to application logs | Critical | CWE-532 | A09:2021 Security Logging and Monitoring Failures |
| SEC-008 | User-controlled Jira domain is used to construct an outbound URL | High | CWE-918 | A10:2021 Server-Side Request Forgery |
| SEC-009 | Generated AI content is inserted into DOM using `dangerouslySetInnerHTML` | High | CWE-79 | A03:2021 Injection |
| SEC-010 | Internal exception details are returned to clients | Medium | CWE-209 | A05:2021 Security Misconfiguration |
| SEC-011 | Jira response/body data is logged extensively | Medium | CWE-532 | A09:2021 Security Logging and Monitoring Failures |
| SEC-012 | Swagger advertises an API-key scheme that is not actually enforced | Medium | CWE-306 | A05:2021 Security Misconfiguration |
| SEC-013 | Swagger UI is enabled without environment restriction | Medium | CWE-215 | A05:2021 Security Misconfiguration |
| SEC-014 | Dependency vulnerability state requires registry-backed scan | Pending scan | CWE-1104 | A06:2021 Vulnerable and Outdated Components |

---

## 3. Detailed Findings

### SEC-001 — Hard-coded Gemini API Key

**Severity:** Critical  
**CWE:** CWE-798 — Use of Hard-coded Credentials  
**OWASP:** A02:2021 — Cryptographic Failures

**Exact location:**  
`J-Test/J-Test/Controllers/UserStoryDescription.cs`

The Gemini API key is declared as a constant directly in source code and is subsequently appended to the Gemini request URL.

**Risk:** Anyone with access to the repository, source archive, commit history, build artifacts, or leaked source can obtain and potentially misuse the credential.

**Required remediation:** Rotate/revoke the exposed key and retrieve the replacement from secure configuration/secrets storage. Never commit the replacement secret.

---

### SEC-002 — Plaintext Password Storage and Comparison

**Severity:** Critical  
**CWE:** CWE-256 — Plaintext Storage of a Password; CWE-916 — Use of Password Hash With Insufficient Computational Effort  
**OWASP:** A02:2021 — Cryptographic Failures

**Exact locations:**
- `J-Test/J-Test/Models/Credential.cs`
- `J-Test/J-Test/Controllers/AuthController.cs` — `Signup`
- `J-Test/J-Test/Controllers/AuthController.cs` — `Login`

Signup persists the supplied password through the `Credential` entity without password hashing. Login performs direct string comparison:

`loginCredential.password != user.password`

**Risk:** Database compromise exposes user passwords directly and may enable credential reuse attacks against other services.

**Required remediation:** Store a one-way adaptive password hash using ASP.NET Core's supported password hashing facilities. Never return password hashes through APIs.

---

### SEC-003 — Missing Effective Authentication and Authorization

**Severity:** High  
**CWE:** CWE-306 / CWE-862  
**OWASP:** A01:2021 — Broken Access Control

**Exact locations:**
- `J-Test/J-Test/Program.cs`
- `J-Test/J-Test/Controllers/AuthController.cs`
- `J-Test/J-Test/Controllers/UserStoryDescription.cs`

`Program.cs` calls `UseAuthorization()`, but the supplied application does not configure an authentication scheme and the controllers do not enforce authenticated access with authorization attributes.

**Affected operations include:**
- listing credentials
- listing Jira connection details
- saving Jira connection details
- deleting credentials
- deleting Jira details
- invoking external integrations

**Risk:** An unauthenticated caller able to reach the API can invoke sensitive operations.

**Required remediation:** Introduce actual authentication, configure the middleware, and apply authorization to protected endpoints.

---

### SEC-004 — Password-Bearing Credential Objects Exposed by API

**Severity:** High  
**CWE:** CWE-200 — Exposure of Sensitive Information  
**OWASP:** A01:2021 — Broken Access Control

**Exact location:**  
`J-Test/J-Test/Controllers/AuthController.cs` — `GetCredentials()`

The endpoint returns `_context.Credentials.ToListAsync()` directly. The entity contains `email` and `password`.

The frontend masking of the password in `Settings.js` is presentation-only; the sensitive value has already been transferred to the browser.

**Required remediation:** Remove this endpoint if unnecessary or return a dedicated safe DTO containing only non-sensitive fields.

---

### SEC-005 — Jira API Tokens Stored and Returned as Plaintext

**Severity:** High  
**CWE:** CWE-312 / CWE-200  
**OWASP:** A02:2021 — Cryptographic Failures

**Exact locations:**
- `J-Test/J-Test/Models/Credential.cs` — `Detail.apiToken`
- `J-Test/J-Test/Controllers/AuthController.cs` — `SaveCredentials`
- `J-Test/J-Test/Controllers/AuthController.cs` — `Getdetails`

Jira API tokens are persisted directly and `Getdetails()` returns complete `Detail` entities.

**Risk:** Database or API exposure can disclose reusable Jira credentials.

**Required remediation:** Do not expose tokens in read responses. Protect persisted tokens using an appropriate secret-management/encryption design or avoid persistence where feasible.

---

### SEC-006 — Jira API Token Sent in URL Query String

**Severity:** High  
**CWE:** CWE-598 — Use of GET Request Method With Sensitive Query Strings  
**OWASP:** A02:2021 — Cryptographic Failures

**Exact location:**  
`J-Test/j-test-ui/src/Components/ImportDataSidebar.js`

The frontend constructs:

`...ExportAllUserStories?...&username=${username}&apiToken=${apiToken}`

**Risk:** Query strings can be retained in browser history, proxy/access logs, diagnostics, telemetry, and other intermediary systems.

**Required remediation:** Do not place credentials in URLs. Send the required data in a protected request body or redesign the backend to use server-side stored credentials.

---

### SEC-007 — Jira Authorization Credential Logged

**Severity:** Critical  
**CWE:** CWE-532 — Insertion of Sensitive Information into Log File  
**OWASP:** A09:2021 — Security Logging and Monitoring Failures

**Exact location:**  
`J-Test/J-Test/Controllers/UserStoryDescription.cs`

The controller constructs a Base64 representation of `username:apiToken` and logs:

`Authorization: Basic {authValue}`

Base64 encoding is reversible and provides no confidentiality.

**Risk:** Anyone with access to application logs may recover the Jira username/API token.

**Required remediation:** Remove credential logging completely. Rotate any token that may already have been captured in logs.

---

### SEC-008 — User-Controlled Jira Domain Used for Server-Side Request

**Severity:** High  
**CWE:** CWE-918 — Server-Side Request Forgery (SSRF)  
**OWASP:** A10:2021 — Server-Side Request Forgery

**Exact location:**  
`J-Test/J-Test/Controllers/UserStoryDescription.cs` — `ExportAllUserStories`

The server constructs an outbound URL using `request.domain`:

`https://{request.domain}/rest/api/2/search...`

No explicit host allowlist or trusted Jira-domain validation is visible.

**Risk:** An attacker may be able to influence the backend into making outbound HTTPS requests to unintended hosts.

**Required remediation:** Validate against an explicit Jira host policy/allowlist and construct URLs using safe URI handling.

---

### SEC-009 — Untrusted/AI-Generated HTML Rendered with `dangerouslySetInnerHTML`

**Severity:** High  
**CWE:** CWE-79 — Improper Neutralization of Input During Web Page Generation (XSS)  
**OWASP:** A03:2021 — Injection

**Exact location:**  
`J-Test/j-test-ui/src/Components/ChatBox.js`

Generated content is converted from Markdown and inserted using:

`dangerouslySetInnerHTML={{ __html: message.text }}`

The source of the generated output ultimately includes user-controlled story content and externally generated model content.

**Risk:** Unsanitized HTML can create DOM-based/cross-site scripting exposure.

**Required remediation:** Sanitize rendered HTML with a vetted sanitizer or render Markdown using a configuration/component that does not permit unsafe HTML.

---

### SEC-010 — Internal Exception Details Returned to Clients

**Severity:** Medium  
**CWE:** CWE-209 — Generation of Error Message Containing Sensitive Information  
**OWASP:** A05:2021 — Security Misconfiguration

**Exact locations:**
- `J-Test/J-Test/Controllers/AuthController.cs`
- `J-Test/J-Test/Controllers/UserStoryDescription.cs`

Responses include patterns such as:

`Internal server error: {ex.Message}`

**Risk:** Internal implementation, database, networking, or external-service information may be disclosed.

**Required remediation:** Log detailed exceptions server-side and return a generic error response to the client.

---

### SEC-011 — Jira Response Content Logged

**Severity:** Medium  
**CWE:** CWE-532  
**OWASP:** A09:2021 — Security Logging and Monitoring Failures

**Exact location:**  
`J-Test/J-Test/Controllers/UserStoryDescription.cs`

The controller logs the Jira API response body. Jira issue content can contain internal project/user-story information.

**Required remediation:** Avoid logging full response payloads. Log only safe operational metadata such as status code, request correlation ID, and result count.

---

### SEC-012 — Swagger Security Definition Is Not Enforced

**Severity:** Medium  
**CWE:** CWE-306  
**OWASP:** A05:2021 — Security Misconfiguration

**Exact location:**  
`J-Test/J-Test/Program.cs`

Swagger defines an `ApiKey` security scheme and requirement, but no corresponding API-key authentication/validation middleware is configured.

**Risk:** API documentation can imply protection that does not exist.

**Required remediation:** Replace the documentation-only scheme with the authentication mechanism actually implemented, or implement the documented mechanism.

---

### SEC-013 — Swagger Enabled Without Environment Restriction

**Severity:** Medium  
**CWE:** CWE-215 — Insertion of Sensitive Information Into Debugging Code  
**OWASP:** A05:2021 — Security Misconfiguration

**Exact location:**  
`J-Test/J-Test/Program.cs`

`UseSwagger()` and `UseSwaggerUI()` execute unconditionally.

**Risk:** Production deployment can expose endpoint discovery and API metadata unnecessarily.

**Required remediation:** Enable Swagger only for approved environments or protect it appropriately.

---

### SEC-014 — Dependency Vulnerability Status Requires Registry-Backed Scan

**Severity:** Pending scan  
**CWE:** CWE-1104  
**OWASP:** A06:2021 — Vulnerable and Outdated Components

**Exact manifests:**
- `J-Test/j-test-ui/package.json`
- `J-Test/j-test-ui/package-lock.json`
- `J-Test/J-Test/J-Test.csproj`

The repository contains a substantial npm dependency tree and multiple NuGet dependencies. A source-only review cannot establish current CVE status reliably.

During this assessment, the npm audit endpoint was unreachable from the analysis environment and the .NET SDK was unavailable. The exact commands required to finish this finding are provided separately.

**Required remediation:** Run registry-backed npm/NuGet vulnerability scans, record every direct/transitive finding, classify them, and remediate Critical/High results through the agent workflow.

---

## 4. Priority for Remediation

The reusable remediation skill should address findings in this order:

1. SEC-001 — exposed Gemini credential
2. SEC-007 — Jira credentials in logs
3. SEC-002 — plaintext application passwords
4. SEC-003 — missing authentication/authorization
5. SEC-004 — password-bearing API response
6. SEC-005 — Jira token storage/API exposure
7. SEC-006 — Jira token in URL
8. SEC-008 — SSRF risk
9. SEC-009 — XSS risk

These are the Critical/High findings. Medium findings can be remediated afterwards, although several are low-effort improvements.

## 5. Evidence Preservation

Before changing application code:

1. Commit this baseline report.
2. Preserve the current source commit/hash.
3. Capture the local dependency-scan outputs.
4. Do not manually repair findings.
5. Create the reusable remediation skill.
6. Make all Critical/High changes through the agent workflow.
7. Re-run the same scans after remediation.
8. Run functional/regression tests and preserve the results.

This produces the required before/after evidence for the re-engineering PoC.
