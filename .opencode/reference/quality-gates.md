# Quality gates

Use this file as the shared source of truth for release-ready QA thresholds. Packs and examples should point here instead of repeating the gate values inline.

## Canonical thresholds

| Dimension | Target |
| --- | --- |
| Core Web Vitals | LCP < 2.5s |
| Core Web Vitals | FID < 100ms |
| Core Web Vitals | CLS < 0.1 |
| Accessibility | WCAG 2.1 AA |
| Security | 0 high/critical vulnerabilities |

## How to use these gates

- Use them for release readiness, browser QA, accessibility review, performance checks, and deployment evidence.
- Keep raw evidence attached to the work: screenshots, browser snapshots, logs, Lighthouse output, vulnerability reports, and concise pass or fail notes.
- Measure against real flows and real environments that match the change risk.

## DESIGN.md reference evidence

When UI work uses external DESIGN.md reference material, evidence must show how the reference was interpreted instead of copied:

1. Record the selected slug from `.opencode/reference/design-md-catalog.md`, plus whether it was primary or secondary.
2. List the transferable patterns extracted from the reference, such as density, hierarchy, contrast, interaction tone, or component affordance.
3. Describe the adaptations made for the user's product, existing tokens, components, content, and platform conventions.
4. Include anti-copying checks for trademark, brand, logos, trade dress, proprietary type, exact layouts, screenshots, and marketing copy.
5. Include accessibility checks for semantics, keyboard use, focus order, labels, contrast, and motion where relevant.

## Evidence checklist

1. Functional flow evidence for the highest-risk user journeys.
2. Accessibility evidence for semantics, keyboard use, focus order, labels, and contrast.
3. Performance evidence for CWV, Lighthouse, and obvious regressions on key pages.
4. Security evidence for auth, authorization, headers, input handling, and dependency scanning.
