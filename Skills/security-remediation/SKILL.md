# J-Test Security Remediation Skill

## Purpose

Secure the J-Test React + ASP.NET Core application while preserving existing
functional behaviour.

All Critical and High security findings must be remediated through this skill.
Do not make unrelated architectural or functional changes.

## Mandatory Workflow

For every security finding:

1. Read the baseline security finding.
2. Inspect all affected code before modifying anything.
3. Explain:
   - vulnerability
   - CWE
   - OWASP category
   - affected files
   - proposed remediation
   - possible functional impact
4. Apply the minimum secure code change.
5. Build the affected application.
6. Run relevant tests.
7. Verify that the vulnerable pattern no longer exists.
8. Record the remediation result.

Do not silently fix unrelated issues.

---

## Rule 1 — Secrets and API Keys

Never hardcode:
- API keys
- passwords
- access tokens
- authentication headers
- connection secrets

Secrets must come from secure runtime configuration.

For ASP.NET Core:
- use IConfiguration / options
- development secrets may use User Secrets
- production secrets must come from environment variables or an approved
  secret store

Never commit real secret values.

If an existing secret has been committed, removal from current source is not
sufficient. Mark it for rotation/revocation.

---

## Rule 2 — Password Security

Never store application passwords in plaintext.

Never perform direct plaintext password comparisons.

Use ASP.NET Core supported password hashing based on:
IPasswordHasher<TUser>.

Signup must:
1. validate input
2. hash the password
3. persist only the hash

Login must:
1. locate the user
2. verify the submitted password against the stored hash
3. never expose the stored hash

Passwords or password hashes must never be returned through API responses.

---

## Rule 3 — Authentication and Authorization

Sensitive endpoints must not rely only on frontend navigation or UI controls.

Authentication must be enforced by the backend.

ASP.NET Core authentication middleware must be configured before authorization.

Protected endpoints must require authorization.

Public endpoints such as signup/login must be explicitly identified.

Do not claim an endpoint is protected merely because Swagger contains a
security definition.

---

## Rule 4 — Sensitive API Responses

Never return persistence entities containing:
- passwords
- password hashes
- API tokens
- secrets
- authentication headers

Create dedicated response DTOs containing only fields required by the client.

Frontend masking is not a security control.

Sensitive fields must be removed before serialization.

---

## Rule 5 — Jira API Tokens

Jira API tokens must never:
- appear in URLs
- appear in query strings
- be logged
- be returned unnecessarily from APIs
- be exposed through exception messages

Prefer server-side credential handling.

If the client must submit sensitive information, use an HTTPS request body
rather than URL parameters.

Persisted Jira tokens must use an approved protection mechanism or secret
store where appropriate.

---

## Rule 6 — Secure Logging

Never log:
- passwords
- API keys
- Jira tokens
- Authorization headers
- Base64 encoded credentials
- complete sensitive external-service payloads

Base64 is encoding, not encryption.

Log operational metadata instead:
- correlation/request ID
- HTTP status
- duration
- result count
- safe identifiers

---

## Rule 7 — SSRF Prevention

Never directly construct outbound service URLs from unrestricted user input.

For Jira:
1. validate the supplied host/domain
2. reject malformed hosts
3. reject unexpected URI schemes
4. restrict outbound requests to approved Jira hosts/domains where possible
5. construct URLs using URI APIs rather than unsafe string concatenation

Do not allow user-controlled values to redirect server-side HTTP requests to
arbitrary destinations.

---

## Rule 8 — XSS Prevention

Never assume AI-generated content is trusted.

Never insert unsanitized:
- user input
- Jira content
- AI-generated content
- Markdown-generated HTML

into the DOM.

If HTML rendering is required, sanitize it using an established maintained
sanitization library.

Prefer safe React rendering where HTML interpretation is unnecessary.

Use of dangerouslySetInnerHTML requires sanitized input.

---

## Rule 9 — Error Handling

Do not return:
- exception messages
- stack traces
- database errors
- internal service details

to clients.

Detailed errors belong in server-side logs.

Client responses should use generic messages and appropriate HTTP status codes.

---

## Rule 10 — Dependency Security

Do not blindly run automated dependency fixes.

For each Critical or High dependency vulnerability:

1. identify direct or transitive dependency
2. record current version
3. identify fixed version
4. identify possible breaking changes
5. upgrade using the smallest safe change
6. restore/install dependencies
7. build
8. test
9. rerun vulnerability scan

Do not introduce unrelated major upgrades during security remediation unless
required to resolve the vulnerability.

---

## Rule 11 — Framework Compatibility

Packages incompatible with the target framework must be reviewed.

For J-Test, compatibility warnings involving packages such as Jira.SDK or
RestSharp must not be ignored.

If an incompatible package is unused, prefer removing it.

If it is required, replace it with a currently supported equivalent after
checking functional impact.

---

## Rule 12 — Frontend Dependency Integrity

Source imports and package manifests must agree.

Do not solve missing-package errors by installing arbitrary versions.

Determine:
1. whether the dependency is actually required
2. whether it is already intended by the application
3. a compatible maintained version
4. security implications
5. whether a safer implementation removes the dependency entirely

Then update the manifest through the agent workflow.

---

## Rule 13 — Swagger / API Documentation

Swagger security definitions must reflect actual authentication.

Do not document security that the application does not enforce.

Production Swagger exposure must be explicitly controlled.

---

## Rule 14 — Minimal Change

Security remediation must preserve existing J-Test functionality.

Do not:
- redesign the UI unnecessarily
- rename APIs without need
- change business behaviour unrelated to security
- refactor unrelated code
- perform cosmetic changes during security remediation

Prefer the smallest secure change.

---

## Rule 15 — Validation After Every Remediation

Backend changes:

dotnet restore
dotnet build

Frontend changes:

npm test -- --watchAll=false
npm run build

Known baseline failures must be distinguished from newly introduced failures.

The baseline currently includes frontend dependency/test/build failures.
Do not attribute pre-existing failures to security remediation.

---

## Rule 16 — Security Rescan

After Critical and High remediation, rerun:

Frontend:

npm audit

Backend:

dotnet list J-Test.csproj package --vulnerable --include-transitive

Also search source for:
- hardcoded secrets
- Authorization logging
- plaintext password comparisons
- sensitive query-string parameters
- unsafe HTML rendering

Compare results against the baseline security report.

---

## Rule 17 — Remediation Evidence

For every finding create an evidence entry containing:

- Finding ID
- Severity
- CWE
- OWASP mapping
- Original vulnerability
- Files changed
- Agent remediation
- Validation performed
- Build result
- Test result
- Rescan result
- Final status

Allowed final statuses:

- CLOSED
- PARTIALLY REMEDIATED
- ACCEPTED RISK
- NOT APPLICABLE

Critical and High findings should be CLOSED unless a documented technical
constraint prevents remediation.

---

## J-Test Remediation Priority

Process the baseline Critical/High findings in this order:

1. SEC-001 — Hard-coded Gemini API key
2. SEC-007 — Jira authorization credential logging
3. SEC-002 — Plaintext application passwords
4. SEC-004 — Password-bearing credential API response
5. SEC-006 — Jira token in query string
6. SEC-005 — Jira token storage/API exposure
7. SEC-003 — Missing authentication/authorization
8. SEC-008 — Jira domain SSRF exposure
9. SEC-009 — Unsafe AI-generated HTML rendering

After Critical/High findings are closed, process Medium findings separately.

## Definition of Done

A remediation is complete only when:

1. vulnerable code is removed or protected
2. affected project builds, or any failure is proven to be pre-existing
3. relevant tests have been executed
4. security rescan no longer reports the vulnerable pattern
5. functional behaviour remains equivalent
6. remediation evidence is recorded