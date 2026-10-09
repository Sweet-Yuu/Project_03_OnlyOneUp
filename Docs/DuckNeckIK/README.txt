DUCK NECK IK

POSE MODE
CTRL_Beak: translate to place the upper beak tip; rotate to orient the head about that contact. Scale is locked.
CTRL_NeckPole: move to choose the neck bending plane. Keep it behind the neck and away from the line between neck base and head target; crossing that line is a pole singularity.
CTRL_Body: move the body while CTRL_Beak stays planted in armature space. CTRL_Beak and CTRL_NeckPole have no parent. Moving the Armature object moves all controls too.

Contact = Beak vertex 37249, the front center of the upper beak. No nest was present; place the nest rim at this point. Lower jaw still uses Jaw.
MCH_HeadTarget is offset from CTRL_Beak by the measured head-to-beak vector (length 0.820534). Three-bone IK on Neck_03 targets this pivot; Head copies its orientation. No cycle, no head pivot substitution, no stretch.
Original deform names, parents, rest transforms and modifiers are retained. Only Body neck weights (rest z 1.78 to 2.96) were smoothed. No additional deform bones.
Local X bends front/back, Y follows the bone length (IK twist locked), Z bends sideways. X limits: first neck +/-155 degrees, others +/-110. Z limits +/-75. The preferred neck pose seeds a smooth forward arch.

LIMITS
Reach depends on both beak position and orientation. Total neck bone length is about 0.989628 scene units. Outside reach the beak will stop short instead of stretching. No collision solver: extreme compression or head rotation can intersect the body. Avoid a collinear pole or flipping it across the neck. Three deform segments remain a stylized rig, not a collision-aware anatomical neck.

GAME EXPORT
Blender constraints do not become engine IK automatically. Bake evaluated animation onto deform bones for clips, or recreate runtime multi-bone IK and the head-to-beak offset in the engine. The imported hierarchy includes CTRL_Root/CTRL_Body ancestors; preserve those ancestors or explicitly retarget an export copy. Do not apply the Armature modifier. This rig adds no Python handlers or runtime script dependency.

QA
See test_report.json, validation.json and the ten rendered pose previews beside this blend. Duck_Original_Snapshot.blend preserves the initial unsaved scene. Saved rig opens in its neutral pose.
