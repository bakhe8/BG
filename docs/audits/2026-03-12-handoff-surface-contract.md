# 2026-03-12 Handoff Surface Contract

Purpose:
- إغلاق `UX-4`.
- تثبيت contract موحد لمساحات `Operations` و`Dispatch`.

## Operations Contract

The queue item must answer:
- لماذا وصل هذا العنصر إلى التشغيل؟
- ما الذي يجب مطابقته أو تأكيده الآن؟
- ما الذي يغلقه؟

The live queue owns:
- scenario
- category
- recommended lane
- capture provenance summary
- system match suggestions
- apply action

Reference-only workflow templates:
- remain available
- move to secondary expandable reference

## Dispatch Contract

Ready items must answer:
- لماذا هذا الطلب هنا؟
- هل يحتاج طباعة، إرسال، أو الاثنين؟
- ما الذي يحدث بعد خروج الخطاب؟

Pending delivery items must answer:
- لماذا بقي هذا العنصر عندي؟
- ما دليل الإرسال الحالي؟
- ما الذي يغلقه الآن؟

## Acceptance Closure

`Operations` no longer mixes:
- live queue work
- workflow reference
at the same level.

`Dispatch` now explains:
- current handoff state
- next required action
- next owner/state after action
for both ready and pending-delivery items.
