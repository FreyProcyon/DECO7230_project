# Final Reflection Documentation

Final report and reflection
Prototype context
Imagine you are a remote operator controlling a lunar 3D-printing rover.
This VR interface is a next-generation XR engineering tool inspired by Rhino/CAD. In IP3 it supports: Create (cube/sphere/cylinder/plane) with preview & grid/stacking → Select → Move / Rotate / Scale → Delete, plus a new Change Material feature.
Objectives & success criteria (plain)
We wanted to check three things:
1.	Basic building: create different objects (and same shape, different sizes), then edit existing ones (move/rotate/scale/delete).
2.	Change Material: can people find it and feel it’s a material change, not just color?
3.	UI + movement: do Poke/Quick Actions feel discoverable and is Teleport comfortable?
Targets (from the test plan): task success ≥80%, each task ≤60s, key ratings ≥3.5/5, and “find it without help” ≥70%. (This round used screen-recording only; exact task times were not captured.)
Method
N=10 (classmates/tutors, mixed experience). Think-aloud + task-based. Screen recording only, no paper notes. Tasks: create; edit (move/rotate/scale/delete); change material. Ratings after tasks.



Results
Participant background (n=10)
•	3D modeling experience: None 40% • Beginner 20% • Advanced 40%.
Overall task ease (self-report, 5-pt)
•	“I can complete the tasks”: mostly 4 or 5; mean ≈ 4.2/5 (n=10).
Create preview & snapping helpfulness (5-pt)
•	Mostly 4–5; mean ≈ 4.3/5 (n=10).
Teleport ease (5-pt)
•	Distribution: 2 (10%) • 3 (30%) • 4 (20%) • 5 (40%); mean ≈ 3.9/5 (n=10).
Change Material ease (5-pt)
•	5/5 from all who answered (n=3; small sample).
Representative comments (short quotes)
•	“Rotation should allow X/Y/Z axis and angle steps.”
•	“Please tune down drop sensitivity when placing.”
•	“Material change looks clearly different, not just color.”
•	“Teleport is fine, but looking up at tall stacks is hard.”
•	“Buttons are big, but hint text is too small; the red text looks like an error.”
(Full quotes and screenshots are in the appendix; ratings charts are from the survey screenshots.)



Synthesis / insights
1) Modeling loop feels nearly complete; two gaps hurt flow.
People can create, preview, snap, and stack (high helpfulness scores). The loop slows down when rotation needs axis control / snapping and when placement feels too sensitive at drop time.
2) Spatial mobility is mostly comfortable, but vertical work is awkward.
Teleport is rated ~3.9/5 and works for ground-level tasks. When stacks get tall, users miss a vertical step or floor anchor, so observing/aligning high parts is harder.
3) Discoverability depends on text affordances.
Large buttons help, but small, red hint text was mistaken for errors. Key entries like Delete and Material palette need clearer labels/contrast and a light “entrance” cue or haptic ping.
4) Material feels like “material,” not just color (promising but thin sample).
The Change Material feature reads well to users; all three respondents rated it 5/5. We need a larger n to confirm.



Evaluation of aims
•	Basic building usable → Mostly met. Users could create, snap, stack, and edit. Rotation precision and drop sensitivity are the two main frictions.
•	Change Material discoverable & meaningful → Directionally met, small n. Users who tried it understood it and noticed a material-level change. We should sample more users.
•	UI discoverability & Teleport comfort → Partly met. Teleport meets the 3.5/5 bar. Discoverability misses our 70% “no-help” target in first-time use because hint text is small and red reads as error.
•	Time per task ≤60s → Uncertain. We did not capture precise timings in this round; next build will log start/finish timestamps in-tool.

Immediate concept updates 
Rotation: add axis toggles (X/Y/Z) and angle steps (e.g., 5°/15°).
•	Placement: reduce drop sensitivity; show a clear “snapped” state; keep 0.1/0.2/0.5 m grid options.
•	Discoverability: enlarge labels (≥32–36 pt), avoid error-red for hints, add a light entrance animation + haptic/audio confirm for Delete and Material palette.
•	Vertical movement: add vertical step or simple floor anchors on top of Teleport.
•	Measurement: log per-task timings and “first-found time” for key UI entries in the next test.



Reflection
1) Prototype Session Review (Based on IP3)
This prototype round focused on the remote control interface for a 3D-printed lunar rover, implemented using a Rhino/CAD-style XR tool: Create → Preview/Mesh → Select → Move/Rotate/Scale → Delete, with the addition of "Change Material".
Effective Aspects:
Participants generally completed the basic modeling tasks, with self-assessment indicating a relatively low difficulty level. Preview and mesh alignment were considered "helpful," suggesting that the "see before you place" workflow reduced the cost of errors. The Teleport was generally well-received, suitable for browsing and manipulating at a ground-scale. "Changing materials" was understood as a change at the "material level" rather than just a color replacement, indicating that the perceptual goals were achievable.
Deficiencies:
When the model became taller or needed alignment at higher levels, spatial mobility was insufficient, making observation and positioning cumbersome; this revealed insufficient consideration of "scene scale changes". The "precision" of rotation and placement was insufficient: participants wanted better control over axes and steps, and more stable landing feedback during placement.

2) Methodological Reflection
This course allowed me to combine rapid prototyping with an executable classroom test plan for the first time: achieving a "measurable core path" at minimal cost, and then collecting comparable data and original statements using Task-based + Think-Aloud methods—this was my biggest takeaway.
I was able to clearly identify three pain points: "spatial mobility," "precision control," and "discovery," and understand how they affect operational efficiency, learning curve, and confidence, respectively. This also made me realize that next time, I should design the measurement mechanism as part of the prototype, rather than remediating it afterward.

3) Concept Evaluation
The initial concept was to move the familiar Rhino/CAD workflow into an immersive scene, allowing the "build, view, modify" loop to occur naturally in the space.
The test results were: Verified aspects: Preview/alignment and step-by-step editing also work in XR, and users can understand and complete the tasks; material switching is effective at the perceptual level, supporting the goal of "making choices like engineering tools."
Partially disproven aspects: Ground-level movement alone is insufficient to cover the editing needs of "high-structure/large-scene" scenarios; furthermore, without precise expression of rotation/placement, users will perceive it as a "playable but not engineering-savvy" tool.
New understanding: The advantage of XR is not simply transplanting desktop functions, but making spatial perspective and real-time feedback an integral part of the workflow; this requires me to emphasize rules and rhythm in "reachability," "precision as feedback," and "first-time discoverability," rather than relying solely on the number of buttons.

4) Improvements and Extensions: 
Previewing before deployment, constraints of meshes/stacks, and "materials as readable decisions" collectively form the skeleton of the "modeling closed loop." If we adopt a different XR technology approach, such as placing some steps in an AR scene and incorporating the relationship between "physical environment and virtual components" into the workflow, we might further reduce the learning cost of spatial awareness; or introduce richer multimodal feedback (such as auditory/tactile) into XR to convey precision, alignment, and state changes.
________________________________________





9) Appendix:
https://github.com/FreyProcyon/DECO7230_project
https://docs.google.com/forms/d/e/1FAIpQLSc07LO-PD9fvtpuXjwgB44Dn8FKF9_c5TrtGdLZj7ukarQNaA/viewform?usp=dialog
  
