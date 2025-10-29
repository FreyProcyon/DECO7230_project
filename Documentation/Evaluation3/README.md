# Evaluation 3 Documentation

Test Plan IP3
1) Prototype Overview
Project: Rhino/CAD-style XR modeling tool (MetaQuest, Interaction SDK/Building Blocks). Background: Controlling a lunar 3D printer to build structures.
Locomotion: Official Teleport
Core Flow: Create (Cube/Sphere/Cylinder/Plane) → Preview/Grid Snap/Stack → Select → Move/Rotate/Scale → Delete; This round adds the Change Material feature and verifies its usability and perceived quality.
________________________________________
2) Objectives
1. Evaluate whether users can efficiently complete basic modeling: creating objects with different attributes, creating objects of the same shape but different sizes, and moving/rotating/scaling/deleting existing objects.
2. Verify the discoverability, operational burden, and subjective feel of the Change Material feature (glossy/rough/metallic, etc.).
3. Continue to verify the discoverability of the Poke UI + Quick Actions and the usability and comfort of Teleport. ________________________________________
3) Methodology
• Participants: Classmates/instructor, target ≥ 5–7 participants.
• Protocol: Think-Aloud + Task-based (3 scheduled tasks).
• Recording: Screen recording only (including hand/controller and UI screens), no paper or pencil required.
• Ratings: 2–3 verbal statements after the task + three 5-point ratings (Teleport, Material Perception).
________________________________________
4) Test Process (≈ 5 minutes/person)
Intro (30 seconds): Explain Teleport; this round adds "Material Change."
Familiarization (1–2 minutes): Freely try out Teleport, Poke, and Create. Task 1 (≤60s)
• Requirements:
a) Create objects of different properties (e.g., different shapes: Cube/Sphere/Cylinder, or different placement properties: on the ground and stacked);
b) Create two objects of the same shape but different sizes (demonstrating mastery of scaling and grid snapping).
• Record: completion/time; whether the scaling control is found without prompts.
Task 2 (≤60s)
• Requirements: Complete Move → Rotate → Scale → Delete (delete from the top bar) on an existing object.
• Record: completion/time; whether all functions can be used smoothly.
Task 3 (New, ≤60s)
• Requirements: Change the material of a selected object, switching to multiple distinctly different materials. Verify that the "material change" is perceptible, not just the "color change." • Record: Completion/time; whether the material palette was switched without prompting;
Wrap-up (30 seconds): Verbal 2–3 overall comments;
Score: UI usability / Teleport comfort / Perceptibility of material changes (1–5 points each).
 ________________________________________
Research summary
Recent work on VR interaction and visual perception suggests that material qualities (e.g., gloss, roughness, metalness) carry strong perceptual and affective cues beyond color, and that clear affordances plus multisensory feedback improve learnability and confidence in immersive tools. Jerald (2015) summarizes human-centered VR guidelines that we apply to keep the material palette large, labeled, and reachable in world space. Slater and Sanchez-Vives (2016) argue that presence is reinforced when actions yield immediate, reliable feedback; therefore, our material change triggers instant visual updates with a short fade and optional haptics for confirmation. Building on material-perception literature (Fleming, 2014), we offer contrasting presets (Matte/Glossy/Metal/Wood) so that users can perceive “material” differences rather than mere color shifts. The IP3 plan tests whether these choices enhance discoverability and perceived material change, while keeping task time low and success relative to high IP2.
References List:
• Fleming, R. W. (2014). Visual perception of materials and their properties. Vision Research, 94, 62–75. https://doi.org/10.1016/j.visres.2014.01.001
• Jerald, J. (2015). The VR book: Human-centered design for virtual reality. Morgan & Claypool.
• Slater, M., & Sanchez-Vives, M. V. (2016). Enhancing our lives with immersive virtual reality. Frontiers in Robotics and AI, 3, 74. https://doi.org/10.3389/frobt.2016.00074

