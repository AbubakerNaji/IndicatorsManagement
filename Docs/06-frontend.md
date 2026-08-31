# 06 — Frontend

React 19 + TypeScript 5.7 + Vite 6 + Tailwind CSS 4, built on the TailAdmin React
template. Lives in [frontend/](../frontend/).

## Layout

```
frontend/src/
  main.tsx              entry: Redux Provider, Theme + Sidebar contexts, App
  App.tsx               every route in one file
  pages/                one component per screen
    AuthPages/          SignIn, SignUp, AuthPageLayout
    Dashboard/ Charts/ Forms/ Tables/ UiElements/ OtherPage/   ← template leftovers
  components/
    auth/               ProtectedRoute, SignInForm, DraftRecoveryModal
    ui/                 Modal, Button, Badge, WorkflowBadge, Table, Pagination, Alert…
    form/               InputField, Select, SearchableSelect, MultiSelect, DatePicker…
    common/             PageMeta, PageBreadCrumb, ComponentCard, ThemeToggleButton
    header/             Header, GlobalSearch, NotificationDropdown, UserDropdown
    charts/ tables/ ecommerce/ UserProfile/                    ← mostly template
  layout/               AppLayout, AppHeader, AppSidebar, Backdrop
  services/             one module per API area (see below)
  store/                Redux Toolkit — authSlice only
  context/              ThemeContext, SidebarContext
  hooks/                useModal, usePagination, useGoBack
  icons/                SVGs imported as components via vite-plugin-svgr
  types/
```

**Template residue.** `pages/Charts`, `pages/Forms`, `pages/Tables`, `pages/UiElements`,
`components/ecommerce` (including `MonthlySalesChart`, `RecentOrders`, `CountryMap`) and
`components/charts` come from TailAdmin and are not part of this product. They are still
routed and still shipped in the bundle — finding **F1**.

## Business pages

| Route | Page | Roles |
|---|---|---|
| `/signin` | `SignIn` | anonymous |
| `/` | `RoleLandingPage` | redirects by role |
| `/dashboard/ministry` | `MinistryDashboard` | Super_Admin, Ministry_Admin |
| `/tasks` | `TaskDashboard` | any |
| `/entries/new` | `EntryWizard` | Super_Admin, Entity_Admin, Data_Entry_User |
| `/review` | `ReviewQueue` | Super_Admin, Ministry_Admin, Entity_Admin, Reviewer |
| `/indicators` | `IndicatorManagement` | Super_Admin, Ministry_Admin |
| `/entities` | `EntityManagement` | Super_Admin |
| `/reporting-periods` | `ReportingPeriods` | Super_Admin |
| `/assignments` | `AssignmentManagement` | Super_Admin, Ministry_Admin |
| `/users` | `UserManagement` | Super_Admin, Ministry_Admin, Entity_Admin |
| `/reports` | `Reports` | any |
| `/publication` | `PublicationManagement` | Super_Admin, Ministry_Admin |
| `/viewer/dashboard` | `ViewerDashboard` | Viewer |
| `/notifications` | `NotificationsCenter` | any |
| `/audit` | `AuditConsole` | Super_Admin, Auditor |
| `/config` | `SystemConfig` | Super_Admin, Ministry_Admin |
| `/profile` | `UserProfiles` | any |
| `*` | `NotFound` | — |

`RoleLandingPage` sends each role to its home screen: ministry roles to the ministry
dashboard, entity roles to `/tasks`, `Reviewer` to `/review`, `Auditor` to `/audit`,
`Viewer` to `/viewer/dashboard`.

## Routing and guarding

All dashboard routes nest inside one `<ProtectedRoute><AppLayout/></ProtectedRoute>`,
with per-route `allowedRoles` on the sensitive ones:

```tsx
<Route path="/indicators" element={
  <ProtectedRoute allowedRoles={["Super_Admin", "Ministry_Admin"]}>
    <IndicatorManagement />
  </ProtectedRoute>
} />
```

`ProtectedRoute` reads `isAuthenticated` and `user.role` from Redux, redirects to
`/signin` when unauthenticated and to `/unauthorized` when the role does not match.

**`/unauthorized` is not a defined route** — it falls through to the `*` catch-all and
renders `NotFound`, so a permission failure looks like a broken link. Finding **F2**.

**Route guards are cosmetic.** They hide screens; they do not protect data. Every
authorization decision that matters is the API's. Never treat a frontend role check as a
security control.

## State

| Concern | Mechanism |
|---|---|
| Auth (user, token, flags) | Redux Toolkit `authSlice`, rehydrated from `localStorage` |
| Theme (light/dark) | `ThemeContext` + `localStorage`, class-based Tailwind toggle |
| Sidebar open/collapsed | `SidebarContext` |
| Server data | Per-component `useState` + `useEffect`; no query cache |

There is no React Query or RTK Query, so list screens refetch on mount and share nothing.
Acceptable at this size; the first thing to reach for if the UI starts feeling slow.

`authSlice` exposes `login` and `logout` thunks, persists `token` and `user` to
`localStorage`, and clears both on logout.

## API layer

[src/services/api.ts](../frontend/src/services/api.ts) is the single Axios instance.

```ts
const API_BASE_URL = import.meta.env.VITE_API_URL || "http://localhost:5117";
const api = axios.create({ baseURL: `${API_BASE_URL}/api/v1` });
```

- **Request interceptor** attaches `Authorization: Bearer <token>` from `localStorage`.
- **Response interceptor** on `401` clears storage and hard-redirects to `/signin`.

One module per API area — `authService`, `dashboardService`, `entryService`,
`indicatorService`, `notificationService`, `publicationService`, `userService` — each
exporting typed functions plus their request/response interfaces.

> **Convention: components never call `axios` directly.** New endpoints get a function in
> the matching `*Service.ts`, with explicit types.

Configure the backend URL in `frontend/.env.local` (see
[.env.example](../frontend/.env.example)); it defaults to `http://localhost:5117`.

## Arabic and RTL

`index.html` sets the direction globally:

```html
<html lang="ar" dir="rtl">
```

IBM Plex Sans Arabic is loaded from Google Fonts. Tailwind's logical properties
(`ps-*`/`pe-*`, `ms-*`/`me-*`, `start-*`/`end-*`) flip automatically with `dir="rtl"`;
prefer them over `pl-*`/`pr-*` so nothing has to be re-mirrored later.

All user-facing copy is Arabic and hardcoded in the components. There is no i18n
framework — deliberate, since the product is Arabic-only, but it does mean the `NameEn`
fields in the domain model have no UI to display them.

## Styling

Tailwind CSS 4 via `@tailwindcss/postcss`. Dark mode is class-based and persisted.
Shared primitives live in `components/ui/`; `WorkflowBadge` renders a `WorkflowState`
with its Arabic label and colour and is the right place to change how states look.

Modals follow one pattern:

```tsx
const { isOpen, openModal, closeModal } = useModal();
<Modal isOpen={isOpen} onClose={closeModal}>…</Modal>
```

See `UserManagement.tsx` or `IndicatorManagement.tsx` for a full CRUD screen worth
copying.

## Build

```bash
npm install
npm run dev       # Vite dev server, port 5173
npm run build     # tsc -b && vite build → dist/
npm run lint      # ESLint
npm run preview   # serve the production build
```

TypeScript runs in strict mode with `noUnusedLocals` and `noUnusedParameters`, so unused
variables fail the build, not just the linter.

Production build output, measured:

```
dist/assets/index-*.css   124.50 kB │ gzip:  20.96 kB
dist/assets/index-*.js    541.08 kB │ gzip: 162.59 kB
```

Vite warns above 500 kB. Everything is in one chunk — no route-level code splitting, and
the unused template pages (charts, calendar, FullCalendar, jVectorMap, Swiper) are inside
it. Finding **F3**.

## Deployment

`frontend/Dockerfile` builds with Node 22 and serves `dist/` from Nginx;
`frontend/nginx.conf` adds the SPA fallback so client-side routes survive a refresh.

`VITE_*` variables are **baked in at build time**, not read at runtime. Changing the API
URL means rebuilding the image.

## Known gaps

| # | Issue |
|---|---|
| **F1** | Unused TailAdmin template pages and components still routed and bundled |
| **F2** | `ProtectedRoute` redirects to `/unauthorized`, which does not exist |
| **F3** | Single 541 kB bundle; no code splitting |
| **F4** | No frontend tests of any kind |
| **F5** | JWT in `localStorage` — readable by any injected script |
| **F6** | No error boundary; a render error blanks the app |
| **F7** | Only `user.role` (singular) is modelled, mirroring the backend's single-role assumption |

Detail and remediation in [13-review-findings.md](13-review-findings.md).
