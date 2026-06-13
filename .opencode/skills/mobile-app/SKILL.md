---
name: mobile-app
description: Deliver cross-platform and native mobile experiences across React Native, Flutter, SwiftUI, and Jetpack Compose without collapsing mobile work into a single framework.
---

# Mobile App

Use this pack for mobile product work across iOS and Android: shared app architecture, screen flows, native capabilities, performance, accessibility, and platform fit.

Defer route choice and lane selection to `../../reference/routing-matrix.md`. For `web/mobile UI`, keep `mobile-app` and `frontend-web` as the primary UI routes, start the harness lane with `visual-engineering`, use `frontend-ui-ux` only when stronger upstream product or interaction judgment helps, and add `impeccable` only as a supplementary refinement layer.

This pack covers both cross-platform and native overlays. Start from the product and device constraints first, then choose the overlay that best matches the codebase. Deprecated wrappers stay non-primary.

When a mobile UI request asks for a specific feel, DESIGN.md references can support visual-language inspiration and token/pattern extraction through `../../reference/design-md-selection-protocol.md` and `../../reference/design-md-catalog.md`. Read the project root `DESIGN.md` first when present, keep `mobile-app` primary, and treat the DESIGN.md layer as supplementary reference material, not a route or helper.

For setup posture, prefer refining an existing project in place. Treat direct `create` / `init` / `new` flows as greenfield-only and use them only when explicitly requested. Defer the full setup policy to `../../reference/project-setup-policy.md`.

## Core focus

- Design mobile flows around reachability, interruption, offline risk, and small-screen clarity.
- Keep navigation, state ownership, and async behavior explicit.
- Treat startup time, memory, battery, and frame stability as first-class quality bars.
- Respect platform conventions instead of flattening iOS and Android into one lowest-common-denominator UX.
- Prefer project tokens, platform conventions, and root `DESIGN.md` before adapting external DESIGN.md references.
- Pair with `architecture-integration` when contract, auth, or sync rules shape the app.

## Shared mobile standards

- Make touch targets generous and predictable.
- Build loading, empty, error, permission, and recovery states for real device conditions.
- Prefer secure storage, least-privilege permissions, and explicit handling of device capabilities.
- Keep background work, notification behavior, and offline sync intentional.
- Validate accessibility, orientation changes, and resumed-session behavior.

## Default workflow

1. Identify the dominant platform surface, project design system, and project root `DESIGN.md` when present.
2. If a named external feel remains useful, follow `../../reference/design-md-selection-protocol.md` and keep any catalog example supplementary to the project design system.
3. Select the nearest overlay in `reference/`.
4. Map navigation, state, sync, permissions, and device capability needs before implementation.
5. Implement primary flows first, then edge states, then performance and platform polish.
6. Add the curated `impeccable` layer when visual refinement or anti-slop review is explicitly needed.
7. Run `review-work` after significant mobile changes.

## Collaboration in this repo

- Use `frontend-ui-ux` only as a supporting upstream helper lane when mobile product or interaction judgment needs a stronger pass.
- Use `Explore` to match local navigation, screen, and component conventions.
- Use `Librarian` or `Context7` for framework and platform APIs.
- Pair with `architecture-integration` for auth, contract, sync, or boundary-heavy work.
- Add the owning backend pack when mobile changes depend on endpoint shape or service behavior.

## Overlays

- `reference/react-native.md`
- `reference/flutter.md`
- `reference/swiftui.md`
- `reference/jetpack-compose.md`

## Guardrails

- Do not assume mobile is only React Native.
- Do not trade platform fit for superficial UI parity.
- Do not ignore startup cost, memory, or interrupted-flow recovery.
