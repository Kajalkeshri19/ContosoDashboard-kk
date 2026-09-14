<!--
Sync Impact Report
- Version change: template -> 1.0.0
- Modified principles: placeholder principles replaced with five ContosoDashboard principles
- Added sections: Additional Constraints; Development Workflow
- Removed sections: none
- Templates requiring updates: [plan-template.md](../templates/plan-template.md) - no structural change required; existing Constitution Check gate remains compatible; [spec-template.md](../templates/spec-template.md) - no update required; [tasks-template.md](../templates/tasks-template.md) - no update required
- Command templates: pending - .specify/templates/commands does not exist in this repository
- Runtime guidance: [README.md](../../README.md) already documents the offline, training-only, security, and abstraction constraints
- Follow-up TODO: confirm the original ratification date
-->

# ContosoDashboard Constitution

## Core Principles

### I. Training-First Scope
ContosoDashboard MUST remain a local, offline-capable training application. Features MUST be
implemented within the existing application architecture and MUST NOT introduce production
claims, external service requirements, or unnecessary infrastructure. Documentation MUST state
material training limitations and security assumptions. This keeps the project available for
instruction while preventing its simplified authentication and deployment model from being
mistaken for a production baseline.

### II. Layered and Cloud-Ready Architecture
Business logic MUST remain separate from UI, persistence, and infrastructure concerns. New
infrastructure dependencies MUST be accessed through focused interfaces when an alternate
implementation is a stated requirement, as with local file storage and future Azure storage.
The default implementation MUST work offline and MUST be registered through dependency
injection. This preserves the current codebase's migration path without coupling training logic
to cloud SDKs.

### III. Secure by Default
Every user-facing capability MUST define authentication and authorization behavior before
implementation. Services and resource access MUST enforce the same ownership, membership, and
role boundaries as the UI; UI checks alone are insufficient. User-controlled paths, filenames,
queries, and uploads MUST be validated, and sensitive files MUST remain outside `wwwroot`.
Security-sensitive changes MUST include a focused verification of unauthorized access, input
validation, and failure cleanup. These rules are non-negotiable even in the training build so
students learn the correct control boundaries.

### IV. Spec-Driven, Incremental Delivery
Each feature MUST have a user-centered specification with independently testable prioritized
stories, a plan with a passing Constitution Check, and dependency-ordered implementation tasks.
Plans and tasks MUST identify concrete repository paths and acceptance checks. Work SHOULD deliver
the highest-priority independently usable slice first. Automated tests MUST be added when the
project has suitable test infrastructure; until then, the plan MUST name executable build,
service, or UI verification steps rather than claiming unverified coverage.

### V. Simplicity and Traceability
Implementations MUST use the smallest design that satisfies the approved requirements and MUST
avoid speculative abstractions, duplicate application architectures, and unrelated refactors.
User-visible behavior, security-relevant actions, and data-changing workflows MUST have clear
acceptance criteria and appropriate operational or audit evidence. Documentation MUST be updated
when a feature changes setup, storage, security, or migration behavior. This keeps training work
understandable and makes decisions reviewable.

## Additional Constraints

- The supported training stack is ASP.NET Core and Blazor Server on .NET 8, with EF Core and SQL
	Server LocalDB unless an approved feature plan documents a change.
- The application MUST run without cloud services or network-only dependencies for normal training
	workflows.
- Mock authentication and authorization MUST remain clearly identified as training-only. Production
	guidance MUST recommend a real identity provider, password protection, MFA, and appropriate
	transport and compliance controls.
- Local uploaded files MUST use generated, non-user-controlled storage names and MUST be stored
	outside the web root. Database metadata and physical file operations MUST have a defined recovery
	path when either step fails.
- Performance targets and data-retention behavior MUST be stated in the feature specification when
	a feature introduces them; unstated targets MUST NOT be implied during implementation.

## Development Workflow

- A feature MUST pass the Constitution Check before design work proceeds and MUST be rechecked after
	design. Any violation MUST be recorded in the plan's Complexity Tracking section with the reason
	and rejected simpler alternative.
- Specifications MUST describe prioritized user journeys, edge cases, functional requirements, and
	measurable outcomes. Plans MUST capture the actual project structure and technical constraints.
- Tasks MUST be organized by user story, include exact paths, preserve dependency order, and identify
	independent validation at each meaningful checkpoint.
- Reviewers MUST verify authorization boundaries, input and file handling, failure cleanup,
	documentation impact, and the feature's stated validation evidence. A build or targeted check MUST
	be run after implementation; a passing review MUST NOT substitute for execution.
- Changes to this constitution MUST update its Sync Impact Report, use semantic versioning, and
	identify affected templates and runtime guidance.

## Governance
<!-- Example: Constitution supersedes all other practices; Amendments require documentation, approval, migration plan -->

This constitution supersedes conflicting project practices for feature specification, planning,
implementation, and review. Amendments MUST be made in a documented change, MUST include a Sync
Impact Report, and MUST update dependent templates or explicitly record why no update is needed.
The amendment author MUST review all existing specifications and plans that could be affected.

Constitution versions use semantic versioning. A MAJOR increment removes or materially redefines a
principle or creates an incompatible governance obligation; a MINOR increment adds a principle or
materially expands mandatory guidance; a PATCH increment clarifies wording without changing
obligations. The Last Amended date MUST use ISO format and reflect the amendment being applied.

Every feature review MUST check compliance with the principles and record any justified exception.
The project owner is responsible for resolving violations before release or documenting an accepted
exception in the plan. Compliance review is required at feature planning, design recheck, and final
validation.

**Version**: 1.0.0 | **Ratified**: TODO(RATIFICATION_DATE): original adoption date is not present in repository history | **Last Amended**: 2026-09-14
