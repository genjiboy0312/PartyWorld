---
name: compass
description: Clarify strategic direction before implementation by reframing ambiguous goals, comparing viable paths, and recommending the next route, skill, helper, or planning step without executing work.
argument-hint: "[goal or ambiguous request]"
user-invocable: true
---

# Compass

`compass` is a top-level user-invocable supplementary orientation skill. Use it for bounded strategic orientation before implementation, not implementation itself.

It reads and defers to `.opencode/reference/routing-matrix.md` before final route recommendations. It hands implementation to the selected route or domain skill.

## When to use

- The request has an unclear goal, broad scope, or several plausible routes.
- The team needs a short option comparison before picking a category, local pack, helper, or plan-agent step.
- The next move may be `Prometheus`, `Oracle`, `review-work`, `architecture-integration`, a domain skill, or a narrower implementation route, but the fit is not clear yet.
- The best answer is a decision brief, not code, edits, tests, or rollout work.

## When not to use

- The route is already clear and the selected domain skill can start.
- The work is a direct implementation, validation, review, research, git, or UI critique task.
- A full plan is needed. Use `Prometheus`.
- A hard quality or architecture challenge is needed. Use `Oracle`.
- Implementation is already complete and needs a finish pass. Use `review-work`.
- The request belongs directly to `architecture-integration` or a domain skill without ambiguity.

## Orientation workflow

1. Reframe the user's goal in one or two plain sentences.
2. List constraints, unknowns, risks, and decisions that affect the route.
3. Read `.opencode/reference/routing-matrix.md` before final route recommendations, keeping the six fixed buckets and helper boundaries intact.
4. Name 2-3 viable directions that could reasonably satisfy the goal.
5. Compare those directions by value, cost, reversibility, risk, and evidence gaps.
6. Recommend one direction and the next route, skill, helper, or plan-agent step.
7. Hand implementation to the selected route or domain skill.

## Output shape

- Goal reframe: restate the strategic target and the user-visible outcome.
- Constraints, unknowns, risks, decisions: separate what is fixed from what still needs judgment.
- Viable directions: give 2-3 viable directions, each with a short label and fit summary.
- Comparison: compare each direction by value, cost, reversibility, risk, and evidence gaps.
- Recommended direction: choose one path and explain why it beats the alternatives.
- Next step: name the next route, skill, helper, or plan-agent step.
- Success criteria: define what must be true when the chosen path is done.
- Verification criteria: define how the chosen path should be checked.
- Question: ask at most one material question only when the answer changes the route or plan.

## Bounded reflection rules

- Keep the pass compact enough to help the next actor move.
- Prefer a clear recommendation when the available evidence is enough.
- Treat unresolved context as an evidence gap, not a reason to expand scope.
- Do not implement, edit files, run broad verification, or create plans from this skill.
- Do not invent new routes, helper IDs, support tiers, or validated surfaces.

## Collaboration in this repo

- Use `Prometheus` when the next step needs a concrete multi-step plan.
- Use `Explore` or `Librarian` when local patterns or source-of-truth context are missing.
- Use `Oracle` when the recommendation needs a harder challenge pass.
- Use `architecture-integration` when the main issue is a system boundary, API contract, auth flow, or cross-stack decision.
- Use domain skills for implementation once the route is chosen.
- Use `review-work` after substantial implementation, not as a substitute for orientation.

## Guardrails

- `compass` is not a primary route, not a routing-matrix replacement, and not an implementation executor.
- It is not a replacement for `Prometheus`, `Oracle`, `review-work`, `architecture-integration`, or any domain skill.
- It keeps route advice separate from public support or validation claims.
- It recommends one next move rather than creating a parallel backlog.
- It stops after orientation and hands work to the selected owner.
