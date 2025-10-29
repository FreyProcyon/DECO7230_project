# Evaluation 2 Documentation
Design Evaluation Report
1) Prototype context — Overview
This is an XR 3D-modeling prototype for Meta Quest. The goal is to let people quickly build simple shapes in VR and arrange them like in Rhino/SketchUp, but with hands/controllers. You can teleport to move around, poke big world-space buttons to open tools, and then create basic shapes (cube/sphere/cylinder/plane). Each shape shows a live preview that snaps to a grid and can stack on top of other objects. After creating, you can select an object and move / rotate / scale it. Unwanted objects can be marked in red and deleted with a top bar. Two editing features (Push/Pull and Change Material) are planned but not finished yet.
________________________________________
2) Objectives & Validation metrics
•	Task success:
o	T1 (Create → Stack → Scale) and T2 (Select → Rotate/Move → Delete) ≥ 80% participants complete; each task ≤ 60s.
•	Discoverability: ≥ 70% can find and use Quick Actions / top Delete bar without hints.
•	Subjective ratings (5-point): Poke UI usability ≥ 3.5/5; Teleport comfort ≥ 3.5/5.
________________________________________
3) Methodology
•	Participants: 7 (classmates + tutor).
•	Procedure: Think-Aloud + two time-boxed tasks; screen recording only (no paper notes).
•	Flow: 30s intro → 1–2min familiarization → T1 & T2 (≤1min each) → 30s verbal feedback + quick ratings.
•	Setup: Meta Quest; ground Teleport Area; world-space Toolbar + Quick Actions (Poke); XR inputs mapped to controller (trigger/thumbstick).
________________________________________
4) Results (factual)
•	Background (n=7): 3D-modeling experience — None 4 (57.1%), Beginner 2 (28.6%), Advanced 1 (14.3%).
•	Questionnaire highlights
o	“I can complete the tasks” (overall): 4.00/5
o	“Preview & grid snapping help placement/stacking”: 4.29/5
o	“I could find and use Toolbar/Quick Actions”: 3.43/5 → if we approximate “agree (≥4)” as “no-hint”, about 42.9% met the bar (below the 70% target).
•	Main verbal feedback
o	Delete has a bug.
o	Push/Pull and Change Material are missing/incomplete.
o	Locomotion feels limited (no vertical fly/ascend).
o	Red onboarding text looks like an error; buttons are big enough but instruction text is too small.
Note: Task times and pass/fail counts were not timestamped this round (screen-recording only), so we don’t report per-task timing/ratios here.
________________________________________
5) Analysis / Insights
•	Create flow is strong: Live preview + grid snapping + stack-on-top produced high confidence (4.29/5).
•	Tool discoverability is the weak link: 3.43/5 average and ~43% “no-hint” indicates labels/visual hierarchy need work (big buttons but small guidance text; red reads as “error”).
•	Feature completeness matters: Missing Push/Pull and Change Material limits users’ sense of a modeling “loop.”
•	Mobility needs a vertical option: Only teleport/walk made viewing tall stacks awkward.
________________________________________
6) Evaluation of aims (against targets)
Aim	Outcome	Verdict
T1/T2 success ≥80%, each ≤60s	Not measured this round (no timestamps)	Undetermined (instrument next round)
Discoverability ≥70% (no-hint)	≈ 42.9% (from Q)	Not met
Poke UI usability ≥3.5/5	Supported by overall task rating 4.0/5 & preview help 4.29/5	Met (directionally)
Teleport comfort ≥3.5/5	Not captured in survey	Add item next round
________________________________________
7) Concept iteration (actionable)
•	Delete flow: finalize XR DeleteTool (state reset, multi-renderer restore, empty-selection handling) and re-test.
•	Onboarding text: change red → neutral/high-contrast (e.g., white/yellow on dark), font size ≥32–36 pt, 10–15s auto-fade; keep a wrist-hint to recall.
•	Visual hierarchy: keep large buttons but add short labels (“Create / Select / Move / Rotate / Scale / Delete”); make the top Delete bar clearly labeled (“Delete selected”) and animate in.
Feature MVPs (next)
•	Push/Pull: axis-locked extrusion with grid steps; simple gizmo or numeric tag.
•	Change Material: 3–4 swatches (solid colors/wood/metal) in world-space palette; tap to apply.
•	Vertical mobility: add vertical step up/down or floor anchors on top of Teleport.
________________________________________
8) Reflection & next steps
•	What worked: Task framing + preview/snapping improved placement confidence; Poke UI broadly usable once discovered.
•	Limitations: Small N; no per-task timing/finish rates; Teleport comfort not surveyed.
•	Next steps:
1.	Ship the fixes (Delete + onboarding text/labels) and re-test with timestamps for T1/T2 time & success.
2.	Add Push/Pull, Change Material, and vertical step.
3.	Extend the survey with a Teleport comfort (1–5) item and a one-line discoverability check.
________________________________________


9) Appendix:
https://github.com/FreyProcyon/DECO7230_project
https://docs.google.com/forms/d/e/1FAIpQLSc07LO-PD9fvtpuXjwgB44Dn8FKF9_c5TrtGdLZj7ukarQNaA/viewform?usp=dialog
 
