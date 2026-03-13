# 2026-03-12 Approvals Surface Contract

Purpose:
- إغلاق `UX-1`.
- تثبيت الفصل بين `Approvals/Queue` كسطح قرار و`Approvals/Dossier` كسطح فحص وتدقيق.

## Queue Contract

The queue exists to answer:
- لماذا هذا الطلب عندي؟
- ما الذي يجب أن أراجعه الآن؟
- ما الذي يحدث بعد القرار؟

The queue may show:
- requester
- request intent
- current stage
- current role
- delegation visibility
- concise blocker explanation
- concise next-step explanation
- direct decision actions

The queue must not show:
- full prior signatures
- full attachment dossier
- full timeline
- detailed policy narrative beyond the current blocker

## Dossier Contract

The dossier exists to answer:
- ما الأدلة الكاملة التي يستند إليها هذا القرار؟
- من وقّع قبلي؟
- ما الذي حدث زمنيًا؟

The dossier owns:
- prior signatures
- attachments
- full timeline
- structured governance and ledger context

The dossier must not execute the decision itself.

## Blocked Decision Explanation Contract

When a decision is blocked, the queue must expose:
- governing policy
- conflicting stage when available
- conflicting actor or responsible signer when available
- immediate hint telling the user that action is not available now

## Acceptance Closure

`Approvals/Queue` now:
- remains action-centered
- keeps dossier counts only as summary
- delegates full evidence to `Approvals/Dossier`

`Approvals/Dossier` now:
- owns the full evidence narrative
- reminds the user to return to queue for the actual decision
