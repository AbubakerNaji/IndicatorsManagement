## ADDED Requirements

### Requirement: Entity-scoped roles reach only their own entity's objects

The system SHALL refuse access to any object belonging to an entity other than the
caller's own when the caller holds an entity-scoped role (`Entity_Admin`,
`Data_Entry_User`, `Reviewer`, `Viewer`).

The caller's entity SHALL be determined solely from the `EntityId` claim on the presented
JWT. It MUST NOT be read from the request body, query string, or route.

This applies to every endpoint that identifies an object by id, not only to list
endpoints.

#### Scenario: Reading another entity's indicator entry

- **WHEN** a `Data_Entry_User` whose `EntityId` is 2 requests
  `GET /api/v1/indicator-entries/{id}` for an entry whose `EntityId` is 3
- **THEN** the system responds `404 Not Found` with `success: false` and the standard
  Arabic message `الإدخال غير موجود`
- **AND** no field of the entry appears in the response

#### Scenario: Reading an entry belonging to the caller's own entity

- **WHEN** a `Data_Entry_User` whose `EntityId` is 2 requests
  `GET /api/v1/indicator-entries/{id}` for an entry whose `EntityId` is 2
- **THEN** the system responds `200 OK` with the entry

#### Scenario: Modifying another entity's entry

- **WHEN** an entity-scoped caller invokes `PUT` or `DELETE` on
  `/api/v1/indicator-entries/{id}`, or any workflow action (`submit`, `approve-entity`,
  `reject`, `return`), for an entry owned by another entity
- **THEN** the system responds `404 Not Found`
- **AND** the entry is left unchanged
- **AND** no notification is sent

#### Scenario: Reading another entity's dashboard

- **WHEN** an entity-scoped caller requests `GET /api/v1/dashboard/entity/{id}` for an
  entity that is not their own
- **THEN** the system responds `404 Not Found`

### Requirement: Ministry-level roles reach every entity

The system SHALL exempt `Super_Admin` and `Ministry_Admin` from entity scoping, because
their remit is explicitly cross-entity.

The system SHALL likewise exempt `Auditor` from entity scoping when reading audit data,
since compliance investigation spans entities by definition.

No other role is exempt.

#### Scenario: Ministry admin reads any entry

- **WHEN** a `Ministry_Admin` requests `GET /api/v1/indicator-entries/{id}` for an entry
  belonging to any entity
- **THEN** the system responds `200 OK` with the entry

#### Scenario: Auditor reads audit records across entities

- **WHEN** an `Auditor` requests `GET /api/v1/audit-logs` without an entity filter
- **THEN** the system returns records from every entity

### Requirement: Refusals do not disclose existence

The system SHALL report an authorization refusal on a by-id endpoint using the same
response as a genuinely missing object: HTTP `404` with the resource's standard Arabic
"not found" message.

The system MUST NOT return a distinct status, message, or timing signal that allows a
caller to distinguish "this id exists but is not yours" from "this id does not exist".

#### Scenario: Refusal is indistinguishable from absence

- **WHEN** an entity-scoped caller requests an id that belongs to another entity, and
  separately requests an id that does not exist at all
- **THEN** both responses have identical status code, `success` value, and `message`

### Requirement: Attachment access derives from the parent entry

The system SHALL authorize every attachment operation — upload, download, delete — by
resolving the attachment's parent `IndicatorEntry` and applying the entity-scoping rule to
that entry.

Attachment endpoints MUST NOT be reachable on the basis of a valid token alone.

#### Scenario: Downloading another entity's attachment

- **WHEN** any caller without a ministry-level role requests
  `GET /api/v1/attachments/{id}/download` for an attachment whose parent entry belongs to
  another entity
- **THEN** the system responds `404 Not Found`
- **AND** no file content is returned

#### Scenario: Viewer cannot download attachments of unpublished entries

- **WHEN** a `Viewer` requests `GET /api/v1/attachments/{id}/download` for an attachment
  whose parent entry has `PublicationStatus = Unpublished`
- **THEN** the system responds `404 Not Found`

#### Scenario: Uploading to another entity's entry

- **WHEN** a `Data_Entry_User` posts a file to
  `POST /api/v1/indicator-entries/{entryId}/attachments` where the entry belongs to
  another entity
- **THEN** the system responds `404 Not Found`
- **AND** no file is written to storage
- **AND** no `attachments` row is created

### Requirement: Entity administrators manage only their own users

The system SHALL restrict an `Entity_Admin` reading or modifying a user by id to users
whose `EntityId` equals the administrator's own.

#### Scenario: Entity admin reads a user outside their entity

- **WHEN** an `Entity_Admin` whose `EntityId` is 2 requests `GET /api/v1/users/{id}` for a
  user whose `EntityId` is 3
- **THEN** the system responds `404 Not Found`

#### Scenario: Entity admin updates a user outside their entity

- **WHEN** an `Entity_Admin` whose `EntityId` is 2 submits `PUT /api/v1/users/{id}` for a
  user whose `EntityId` is 3
- **THEN** the system responds `404 Not Found`
- **AND** the target user is unchanged

### Requirement: Authorization is enforced in the service layer

The system SHALL enforce entity scoping inside the service that loads the object, not in
an ASP.NET Core authorization handler.

An authorization handler can only inspect route and query values, which do not reveal the
owning entity of an object identified by its own primary key. The check MUST therefore
happen after the object is loaded and before any of it is returned or modified.

#### Scenario: Scoping holds regardless of entry point

- **WHEN** an object is reached through any route, including one added in future
- **THEN** the entity-scoping check applies, because it lives in the service method that
  loads the object rather than in per-route configuration

### Requirement: Every scoping rule has a negative test

The system's test suite SHALL include, for every endpoint that identifies an object by id,
a test asserting that a caller from a different entity is refused.

A change that removes or weakens a scoping check MUST cause a test failure.

#### Scenario: Cross-entity access test coverage

- **WHEN** the authorization test suite runs
- **THEN** it contains at least one failing-access assertion for each of
  `GET`/`PUT`/`DELETE /indicator-entries/{id}`, the four workflow actions,
  the three attachment endpoints, `GET /dashboard/entity/{id}`, and
  `GET`/`PUT /users/{id}`
