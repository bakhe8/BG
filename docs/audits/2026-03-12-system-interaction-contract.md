# 2026-03-12 System Interaction Contract

Purpose:
- إغلاق `UX-5`.
- توحيد العقود الدلالية للأفعال والتنبيهات والإدارة.

## Core Action Verbs

Create:
- used for first-time objects such as users, roles, requests, delegations, stages

Submit:
- used when ownership moves from preparer to workflow

Approve / Return / Reject:
- used only inside approval runtime

Apply:
- used when operations confirms external evidence and commits the resulting change

Print / Dispatch / Confirm delivery:
- used only in dispatch lifecycle

Revoke:
- used only for undoing temporary delegation authority

## Notification Semantics

`success`
- action completed
- redirect-safe message

`info`
- explains task boundaries, state meaning, or where details live

`warning`
- blocked or risky condition that still needs user interpretation

`validation`
- action could not be executed because required input is missing or invalid

## Administration Task Model

Users:
- account creation and credential maintenance

Roles:
- permission bundle design

Delegations:
- temporary approval coverage

Workflow:
- executable approval path configuration

## Guardrail

No new feature should introduce:
- a new action verb
- a new notification meaning
- a new object detail surface
without mapping it to this contract first.
