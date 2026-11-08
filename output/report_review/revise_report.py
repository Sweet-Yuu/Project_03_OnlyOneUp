from pathlib import Path
from copy import deepcopy
from docx import Document
from docx.shared import Inches, Pt, RGBColor
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.text.paragraph import Paragraph
import json

ROOT = Path(__file__).resolve().parent
SRC = Path(r'C:/Users/HN/Downloads/EggPress3D_Report.docx')
OUT = ROOT / 'EggPress3D_Report_Updated.docx'
d = Document(SRC)
ps = list(d.paragraphs)
ts = list(d.tables)
changes=[]

def replace(i,text):
    p=ps[i]
    old=p.text
    prop=deepcopy(p.runs[0]._r.rPr) if p.runs and p.runs[0]._r.rPr is not None else None
    p.clear()
    run=p.add_run(text)
    if prop is not None: run._r.insert(0,prop)
    changes.append({'paragraph':i,'before':old,'after':text})
    return p

P={
1:'Project report • Updated to the implementation reviewed on 8 November 2026',
8:'The nine-week plan below groups development work by milestone. It describes the implementation scope and intended sequence; it is not a verified record of completion dates.',
11:'The responsibility table retains the team assignments from the original report. Technical tasks have been updated to match the current modules.',
14:'Team membership and student identifiers are retained from the supplied report. Shared work includes integration, testing, report writing and the presentation.',
26:'Egg Press 3D is a cooperative journey in which two ducks carry a nest containing a fragile egg through a large kitchen and an outdoor obstacle course. Each player controls one duck. Their relative positions determine how the shared nest moves and tilts, so progress depends on communication and careful movement.',
27:'The first playable map is Kitchen_Map, with preparation counters, stove hazards, a seesaw crossing, cold storage, a pantry and a final patio. It combines traversal with pressure plates, a linked-lights puzzle and a symbol-code lock. The second map, MainMap_Online, uses outdoor platforms, bridges, obstacles, checkpoints and a finish area.',
29:'Two players connect as a host and a client. Both use the same duck player prefab and movement abilities. Each has a player name and an independently selected cosmetic item. The host starts the match and resolves shared gameplay state, but both players participate in carrying the nest.',
31:'The game uses cartoon ducks, colourful scenery and a warm orange interface. Cracks, egg-break visuals, screen feedback and the movement of the nest communicate danger. Reaching a checkpoint gives the pair a shorter recovery route after failure.',
36:'A 3D online co-op platformer for two players carrying one nest and egg. Relative movement, height differences, collisions and jump timing can destabilise the egg. Players must navigate the route, solve environmental puzzles and reach the level goal together.',
38:'Menu and equipment selection → create or join a Relay room → host starts with two players → loading and preparation → carry the nest, solve puzzles and collect coins → checkpoint recovery after failure → level victory → next level, replay or return to menu.',
40:'Shared carrying: the nest is positioned from both players’ carrying points. Distance constraints and the duck rig keep the pair visually connected to it.',
41:'Recoverable failure: egg breakage or a player entering a death zone restarts both players and the egg at the current checkpoint, or at the start if no checkpoint has been reached.',
42:'Readable feedback: the egg has discrete damage stages, with cracks and local screen feedback. Falling out of the nest is a separate risk from accumulating damage.',
43:'Personal expression: players buy cosmetic head items with coins, preview them in the menu and display their own selected item during online play.',
50:'The egg is a shared object created and managed by NetworkNest. Its lifecycle uses Waiting, Intact and Broken. DamageCount is separate from the lifecycle phase; an egg with one or two damage increments is still in the Intact simulation phase.',
53:'Table 7: Egg damage stages',
56:'The current levels combine collision geometry, death zones and cooperative puzzles. The following table describes implemented mechanisms rather than the earlier proposed butter, toaster, fan and cooking systems.',
60:'Four scenes are enabled in the build: Menu_Scene, Kitchen_Map, MainMap_Online and Credit. Room creation, the lobby and shop are panels in Menu_Scene. Gameplay scene changes use Netcode’s NetworkSceneManager. Victory is presented as a panel within the gameplay scene.',
63:'Menu_Scene: name entry → Create / Join → room roster → host Start\n↓\nKitchen_Map: loading → carry and solve puzzles → victory panel\n↓ Select Next Stage\nMainMap_Online: loading → outdoor traversal → victory panel\n\nFailure in either map → restart at the active checkpoint.\nQuit or partner disconnect → circular transition → Menu_Scene.',
67:'Figure 2: Main menu from the earlier build',
70:'Figure 3: Earlier Relay debug UI, replaced in the main flow by menu lobby panels',
77:'The egg does not have a 100-point health bar or continuous speed, acceleration or squeeze damage. With progressive damage enabled, eligible damage events increment a counter. Nest collisions can either damage the egg or eject it, as decided by the host.',
78:'When the egg breaks, both players and the nest restart at the saved checkpoint with a reset egg. Entering a configured Dead zone also restarts the pair. There is no score deduction or limited-life counter in the current round logic.',
79:'A checkpoint can be reported by one player; the server validates the contact and safe respawn positions for both. In Kitchen_Map, a player or the nest entering the final trigger can activate victory. In MainMap_Online, NetworkNest checks whether the nest touches Win_Area. The code does not require both ducks and the egg to enter the goal simultaneously.',
80:'Coins and progression',
81:'Coins are the implemented reward currency. A valid online pickup is approved by the server, despawns the shared coin and adds one coin to each connected player’s local wallet. Coins buy cosmetic items in the menu shop. A time score, seed score, restart penalty, star rating and competitive leaderboard are not implemented.',
83:'Agree on a route and communicate before changing direction or stepping onto a narrow platform. Movement is relative to the local camera.',
84:'Keep the players at compatible heights and avoid extreme nest tilt. The egg can roll toward the lower edge and leave the nest when the release angle is exceeded.',
85:'Use jump timing deliberately. The cooperative jump detector pairs timestamps from different players; a matched pair releases the egg from assisted carrying. In the current maps this can push it sideways out of the nest.',
86:'At puzzle consoles, read the prompt and use E to enter or leave input mode. Solve the lights or symbol code before crossing the opened gate. Esc opens settings; H or F1 toggles the tutorial.',
87:'The gameplay interface includes a coin display, player nameplates, tutorial and settings controls, loading transitions and the victory panel. Egg damage is communicated through the model and screen effects.',
89:'',
92:'Kitchen_Map is arranged into six named zones and contains checkpoint IDs 1–6. MainMap_Online is an outdoor platforming course with an active checkpoint configuration. Route length and completion time depend on player coordination; this report does not assign an unmeasured one-minute maximum loss or fixed completion time.',
93:'World 1 — Kitchen_Map',
96:'The kitchen route progresses from the preparation island through the cookline, dishwashing area, cold-storage vault and high pantry to the VIP banquet and patio.',
98:'',99:'The map combines static kitchen props with interactive gates, death zones and a tilting plank. Decorative props should not be interpreted as functional cooking or launch-pad mechanics.',101:'',
102:'World 2 — MainMap_Online',
105:'The outdoor route uses raised platforms, wooden bridge sections, collision obstacles and a death zone. Players must carry the nest through the route and bring it into contact with the finish area.',107:'',
109:'Gameplay parameters are stored in component fields, prefab values and scene overrides. The table below distinguishes the current mechanisms from values proposed in the original design. ShopData is the ScriptableObject catalogue for cosmetics; no shared HazardConfig asset drives all obstacles.',
113:'The project uses URP 17.3.0 with scene lights, materials, shadows and environment assets. Lighting and shadow cost must be assessed per scene and camera view; a fully baked lighting setup is not assumed.',
114:'LocalPlayerCamera binds the active Cinemachine camera to the local player’s tracking target and preserves the scene camera’s lens and input configuration. It is not a single camera shared by both computers or a mandatory Cinemachine Target Group containing both ducks and the egg.',
115:'Audio is organised through scene background music, UI hover/click sounds and the existing audio services. Settings expose Master, Music and SFX volume/mute controls and save these preferences. Audio assigned to puzzle gates and the seesaw provides interaction feedback.',
120:'The egg — runtime components',
123:'Damage and release rules',
124:'The server owns the egg simulation and decides damage. Progressive damage is enabled by overrides on the nest instances in both gameplay scenes. Each accepted damage event changes DamageCount; the implementation uses discrete contact events rather than continuous stress formulas.',
125:'First and second damage events: DamageCount increases to 1 and then 2; the visual shows increasing cracks while the phase remains Intact.',
126:'Third damage event: the egg enters Broken and raises the round-failure event. Duplicate contacts on the same object and contacts in the same physics step are filtered.',
127:'Nest impact: after preparation and cooldown checks, the host chooses between applying a damage increment and ejecting the egg. The shared nest prefab currently sets a 0.5 crack probability and a 0.75-second cooldown.',
128:'Release and landing: ReleaseCarry marks the egg to break at its next eligible environment collision, even if it has fewer than two prior damage increments. Nest and player colliders, triggers and Win_Area are excluded from ordinary environment damage checks.',
129:'Recovery: NetworkRoundManager responds to egg failure with a checkpoint restart. The egg lifecycle also breaks an actively simulated egg below world Y = −50 as a safety fallback.',
130:'Table 16: Current carrying and damage parameters',
132:'Values above were read from the current scripts, NetWorkPlayer/Nest prefabs and gameplay scene overrides. Scene overrides take precedence over prefab defaults; for example, the current jump-pair window is 0.694 seconds in both maps rather than the 0.2-second prefab default.',
133:'Assisted carrying and free flight',
134:'NestEggCarryPhysics simulates a bounded egg offset on the nest floor using gravity, a small inertial contribution and damping. While carried, the body is moved in the nest coordinate system. Excessive tilt or paired jump events release it to server-side Rigidbody motion. There is no simulated internal yolk, raw-to-boiled conversion, player-controlled squeeze or Q/E balancing mechanic.',
136:'NestEggPhase defines Waiting, Intact and Broken. Waiting covers setup; reset activates Intact; breaking activates Broken; a round restart creates the next intact simulation state. DamageCount 1 and 2 do not create separate Cracked or Critical states. Player movement separately uses an Idle/Walk state machine, with jump handled by the motor.',
137:'Waiting → preparation/reset → Intact → break condition → Broken\nBroken → checkpoint restart/reset → Intact\nAny active phase → stop/wait for preparation → Waiting\n\nDamageCount within Intact: 0 → 1 → 2.\nThe next accepted damage event, or an eligible landing after release, produces Broken.',
138:'Figure 8: Egg lifecycle and separate damage counter',
144:'The pair carries the nest through carrying anchors, geometry calculations and distance constraints. The player motor uses CharacterController. The nest follows the shared carry pose, while the egg uses assisted carrying or free Rigidbody motion. The current system does not apply two player-controlled spring grips directly to the eggshell.',
149:'The project contains a rigged duck, nest and egg visuals, kitchen/outdoor environment assets and cosmetic item prefabs. Imported models and decorative assets are distinct from the gameplay scripts that make an object interactive. Exact asset provenance and licences remain in the team’s asset records.',
150:'The existing audio assets support background music, UI actions and configured gameplay interactions. Master, Music and SFX settings are applied through the audio mixer and stored locally.',
151:'The UI uses Unity UI and TextMeshPro for menu/lobby panels, the shop, coin display, settings, tutorial pages, loading and victory feedback.',
152:'Egg feedback uses crack/broken visuals, camera shake and a screen flash. The current report does not claim pooled shell shards, steam or a boiling effect.',
154:'The following examples refer to implemented classes. Interfaces separate input, movement constraints, wallet access, Relay service calls and puzzle interactions. The project also retains direct references and singleton services, so these examples describe specific applications of SOLID rather than claiming that every class follows every principle.',
156:'S — Single Responsibility: KeyboardPlayerInput reads movement/jump input; PlayerCharacterMotor applies movement; PlayerAnimationPresenter controls animation. These responsibilities are separated from session setup and UI.',
158:'O — Open/Closed: PlayerCharacterMotor accepts IPlayerMovementConstraint. The motor can use a different constraint implementation without changing its movement calculation.',
160:'L — Liskov Substitution: OnlineIdleState and OnlineWalkState implement OnlinePlayerState. OnlinePlayerStateMachine calls the same Enter, Tick and Exit contract regardless of the active state.',
162:'I — Interface Segregation: ICoinWallet exposes the wallet operations needed by coin collection; IPlayerMovementConstraint exposes movement limiting; IPuzzleInteractable exposes puzzle activation. Consumers do not depend on a large all-purpose game manager.',
163:'Example: Coin receives an ICoinWallet through Initialize; it calls AddCoin after collection. Puzzle controllers reference IPuzzleInteractable to activate their gate.',
164:'D — Dependency Inversion: RelaySession receives IRelayRoomService in its constructor, and coin collection receives ICoinWallet. Unity-facing installers connect concrete implementations to those contracts.',
167:'State pattern: OnlinePlayerStateMachine changes between OnlineIdleState and OnlineWalkState. Jump input is passed separately to the motor; the egg uses an enum lifecycle rather than the five-state class hierarchy proposed in the original report.',
168:'The state-machine example is Assets/Scripts/Player/Movement/OnlinePlayerStateMachine.cs. ChangeState calls Exit on the previous state and Enter on the next state, while Tick delegates movement decisions to the current state.',
170:'Strategy-style composition: IPlayerMovementConstraint lets PlayerCharacterMotor delegate movement limits. CarryDistanceConstraint supplies the current carrying constraint. This separates movement integration from the rule that keeps two carriers within reach.',
171:'Observer pattern: CurrencyManager raises OnCoinChanged after AddCoin or a successful SpendCoin. CoinUI subscribes, updates its TextMeshPro label and unsubscribes when destroyed. RoomLobbyView similarly listens to roster changes; equipment visuals listen to the equipped-ID NetworkVariable.',
172:'Singleton usage: SaveSystem, CurrencyManager, PlayerInventory and CharacterEquipment provide shared access in the existing implementation. They simplify access but require correct scene lifetime and initialisation. No Object Pool or HazardBase Template Method is claimed for the current gameplay.',
173:'Network synchronisation: player movement is owner-authoritative; shared nest/egg state, checkpoint decisions and round outcomes are server-controlled. NetworkVariables carry persistent state and RPCs carry requests/events. Nest snapshots include both nest and egg data, and the client uses prediction, reconciliation and presentation smoothing.',
174:'Interface-based puzzle design: LinkedLightsPuzzle and SymbolCipherPuzzle use IPuzzleInteractable for the gate. Their plain C# rule classes can be tested without running a scene. EditMode test sources exist for LinkedLightsBoard and SymbolCipherLock.',
175:'2.4 Performance Analysis',
178:'2.5 Online Session Flow',
179:'Create room: RelayRoomService signs in anonymously, allocates room for one joining client and obtains a Relay join code. RelaySession configures UnityTransport and starts the NGO host. The join code is returned by Relay; the project does not generate a fixed four-character Fusion room name.',
180:'Join room: the other player enters the shared Relay code. The session joins through Unity Transport, and RelayRoomRoster maintains the two-player list and names. Invalid or unavailable rooms report an error through the session UI.',
181:'The current menu supports Create and Join by code. Automatic quick matching and skill-based matchmaking are not part of this implementation.',
182:'Start: only the host sees an enabled Start action when both connected players and roster entries are present. There is no separate Ready button required from each player. NetworkGameStarter synchronises the first map load and then spawns each player object with its owner and display name.',
183:'Disconnect or Quit: leaving gameplay ends the two-player session for the other participant as well. ReturnToMenuTransition performs the circular cover/loading effect and returns to Menu_Scene. Reconnection and host migration are not implemented.',
184:'2.4.1 Test Environment',
187:'2.4.2 Performance Targets',
190:'2.4.3 Measurement Method',
191:'Tools',
194:'Capture Scenarios',
195:'The screenshots and numerical results retained below belong to the earlier report. They document a historical capture, not a benchmark of the current scripts and levels. A new comparison must use the same hardware, resolution, quality settings, build type and capture duration for host and client.',
199:'2.4.4 Historical Baseline',
202:'2.4.5 CPU and Presentation Waits',
206:'2.4.6 Issues and Proposed Optimisation',
207:'Draw Calls',
208:'The earlier report records 4,725 batches for a gameplay capture. This is a useful optimisation lead, but the count depends on the scene, camera and render passes. It is not a universal limit or a current-build measurement.',
209:'A high batch count can increase render-thread submission cost. A zero “Saved by batching” statistic alone does not prove that every batching path is disabled; SRP Batcher behaviour must be inspected separately in the Frame Debugger.',
211:'Evaluate static batching for suitable non-moving environment meshes, checking the memory cost and the render-pipeline configuration.',
212:'Evaluate GPU instancing for repeated compatible mesh/material combinations, and compare frame time before and after. Do not assume every repeated object can be reduced to a single draw call.',
213:'Geometry Workload',
214:'The earlier capture reports approximately 9.4 million triangles and 9.8 million vertices. These are recorded rendering statistics and should be remeasured with the current content and camera views.',
215:'High visible geometry can add GPU work. To establish the bottleneck, compare GPU timing and controlled changes to resolution, shadows and geometry. The FPS value and polygon count alone do not prove a GPU-bound frame.',
217:'Review dense models and reduce detail that is not visible at gameplay distance. Preserve the silhouette and important visual features.',
218:'Evaluate LOD Groups for distant environment assets and verify transitions during actual camera movement.',
219:'Evaluate occlusion culling where level geometry can hide substantial content; measure its benefit against the cost and bake requirements.',
220:'Shadow Workload',
221:'The earlier report records 2,333 shadow casters in the captured view. The number is historical and varies with scene visibility and shadow settings.',
222:'Shadow passes render additional geometry from the light’s view. Shadow distance, cascades and the number of visible casters should be measured alongside GPU timing before selecting a change.',
224:'Disable shadow casting on minor or distant decorative objects where it does not affect readability.',
225:'Evaluate baked or mixed lighting for static scenery while retaining suitable dynamic shadows for ducks, the nest and moving objects. Compare quality and performance rather than assuming this conversion is already complete.',
226:'2.4.7 Frame Debugger Observations',
234:'The proposed demo follows the current two-player flow from menu customisation and Relay connection through the kitchen and outdoor maps. The sequence below is a recording plan, not evidence that every test has passed.',
237:'Demo video, repository and playable-build links should be supplied by the team with the final submission.',
238:'Demo capture 1 should show both ducks carrying the nest through a kitchen puzzle, with names and coin UI visible.',240:'',
241:'Demo capture 2 should show egg damage or a fall, followed by both players returning to the active checkpoint.',243:'',
244:'Demo capture 3 should show the victory panel and the selected next-stage, replay or menu action on both machines.',246:'',
247:'3.2 Build and Deployment',
254:'Create a Windows build from Unity Build Profiles using the enabled scene list. Enable Development Build for profiling; create a separate release build after verification.',
262:'The checklist below covers the implemented controls, shared egg behaviour, puzzles, coins, cosmetics and session flow. Results are marked Pending until the team runs each case on the final build. Source review confirms implementation paths, not multiplayer runtime correctness.',
275:'The project separates input, motor, animation, carrying and session concerns, and stores cosmetic catalogue data in ShopData.',
276:'Plain C# puzzle rules and small interfaces support focused testing; C# events and network state-change callbacks update the UI and replicated visuals.',
278:'Shared carrying remains sensitive to latency, jitter and packet loss. Prediction and smoothing are implemented, but no verified latency threshold is claimed without controlled multiplayer tests.',
279:'Authority is mixed: players drive their own movement, while the host resolves the nest/egg and shared outcomes. Cosmetic ownership and currency are stored locally, so this is not a server-secured economy.',
280:'Content consists of two gameplay maps. There is no reconnect/host-migration flow, automatic matchmaking, built-in voice chat, timed leaderboard or star scoring.',
288:'Optional future work: add built-in voice chat or emotes and evaluate a compatible service for the existing Unity networking stack.',
289:'Optional future work: introduce a timer, explicit scoring rules and leaderboards after designing and validating the score model.',
290:'Extend the existing cosmetic shop with additional items, stronger purchase validation and cloud/account persistence if needed. Hats and equipment selection already exist.',
291:'Accessibility work could include colour-independent puzzle cues, control remapping and full gamepad coverage. Current online movement reads keyboard input.',
295:'[2] Unity Technologies. Netcode for GameObjects 2.13 documentation: NetworkVariables and RPCs. https://docs.unity.cn/Packages/com.unity.netcode.gameobjects@2.13/manual/basics/networkvariable.html',
298:'[5] Unity Technologies. Relay allocating, binding and joining. https://docs.unity.com/relay/allocating-binding-joining',
299:'[6] Unity Technologies. Unity Profiler documentation. https://docs.unity3d.com/Manual/Profiler.html',
302:'[9] Project asset folders and their supplied licence files. Asset credits should list the exact imported packages used in the submitted build.',
303:'[10] Egg Press 3D project source, scenes and configuration reviewed on 8 November 2026. Key implementation paths are listed in the appendix.'
}
for i,t in P.items(): replace(i,t)

def table(index,rows,widths=None):
    t=ts[index]
    old=[[c.text for c in r.cells] for r in t.rows]
    hdr=deepcopy(t.rows[0]._tr)
    body=deepcopy(t.rows[1]._tr)
    for row in list(t._tbl.tr_lst): t._tbl.remove(row)
    for ri,values in enumerate(rows):
        t._tbl.append(deepcopy(hdr if ri==0 else body))
        row=t.rows[-1]
        for ci,val in enumerate(values):
            cell=row.cells[ci]
            cell.text=str(val)
            cell.vertical_alignment=1
            for p in cell.paragraphs:
                p.paragraph_format.space_before=Pt(2)
                p.paragraph_format.space_after=Pt(3)
                p.paragraph_format.keep_with_next=False
                for run in p.runs:
                    run.font.name='Calibri'; run.font.size=Pt(10 if len(values)>4 else 10.5)
                    run.bold=(ri==0)
            if widths: cell.width=Inches(widths[ci])
        trpr=row._tr.get_or_add_trPr()
        for h in list(trpr.findall(qn('w:trHeight'))): trpr.remove(h)
        ns=OxmlElement('w:cantSplit'); trpr.append(ns)
        if ri==0:
            repeat=OxmlElement('w:tblHeader'); trpr.append(repeat)
    if widths:
        for col,w in zip(t.columns,widths): col.width=Inches(w)
    changes.append({'table':index,'before':old,'after':rows})

table(1,[['Week','Main tasks','Deliverables'],
['1','Form the team; define two-player carrying and the report scope.','Project concept and responsibilities'],
['2','Define the nest/egg interaction, routes and recovery loop.','Gameplay plan and task breakdown'],
['3','Prototype duck movement, carrying geometry and egg motion.','Movement and carrying prototype'],
['4','Integrate NGO, Unity Transport and Relay; create/join rooms.','Two-player connection and spawning'],
['5','Add owner movement, shared nest snapshots and round preparation.','Online carrying and player presentation'],
['6','Build Kitchen_Map, its puzzles, hazards and checkpoints.','First gameplay map'],
['7','Build outdoor traversal; add round flow, coins and cosmetics.','Second map, recovery and progression'],
['8','Exercise test cases; profile host/client; improve stability.','Test checklist and performance captures'],
['9','Polish UI/audio; verify builds; record demo and finalise report.','Submission build and report']])
table(2,[['No','Member','Role','Detailed tasks'],
['1','Dương\nHE191504','Team Leader; UI/UX, Audio and VFX','Menu, settings, tutorial, lobby and result UI; audio integration; presentation feedback; profiling and report integration.'],
['2','Quang\nHE194563','Network and Gameplay Physics','NGO/Relay sessions, player spawning, nest/egg simulation and synchronisation; checkpoint/round flow; coins, equipment and session tests.'],
['3','Vũ\nHE191675','Level Design and Hazards','Level routes, environment setup, collisions, checkpoints and puzzle/hazard integration; level-design documentation.']],[.3,.8,1.45,3.7])
table(3,[['Item','Description'],['Title','Egg Press 3D'],['Genre','3D cooperative physics platformer'],['Players','Exactly two in the normal online session: host and client'],['Platform','Windows PC; keyboard and mouse'],['Engine / language','Unity 6000.3.15f1; C#; URP 17.3.0'],['Networking','Netcode for GameObjects 2.13.3, Unity Transport and Unity Relay'],['Camera','Local Cinemachine camera bound to the local player’s tracking target'],['Progression','Kitchen_Map → MainMap_Online; checkpoint recovery; coins and cosmetic equipment'],['Audience','Players who enjoy communication, traversal and cooperative puzzles']],[1.4,4.85])
table(4,[['Character','Role','Abilities','Notes'],['Host duck','One carrier; starts the room','Move and jump; interact with puzzle consoles; collect coins','Same movement abilities as client; own name and equipment.'],['Client duck','Second carrier; joins by code','Move and jump; interact with puzzle consoles; collect coins','Owns its player input and movement; sees shared nest/egg state.']],[1,1.2,2,2.05])
table(5,[['Property','Assisted carry','Released egg'],['How it starts','Round preparation resets the egg on the nest.','Tilt, paired jumps or an ejection event ends assistance.'],['Motion','Bounded rolling offset in nest coordinates.','Host simulates free Rigidbody motion.'],['Physics body','Kinematic during assisted movement.','Dynamic while freely simulating.'],['Damage','Eligible nest-impact branch can add damage.','Eligible landing after release breaks the egg.'],['Network view','Nest and egg are sent together in snapshots.','Same shared snapshot/presentation path.']])
table(6,[['Damage / phase','Visual state','Effect'],['0 / Intact','Undamaged shell','Normal carrying simulation.'],['1 / Intact','First crack stage','One accumulated damage event.'],['2 / Intact','Further damage','Next accepted damage event breaks the egg.'],['3 / Broken','Broken egg visual','Round failure and checkpoint recovery; release landing can reach this state immediately.']])
table(7,[['Map','Obstacle / puzzle','Interaction','Effect'],['Kitchen','Linked lights','Console','Toggle a 3×3 board to all-off to open its gate.'],['Kitchen','Cookline plates','Two pressure plates','Activate both within the grace rule to remove the hazard barrier and open the gate.'],['Kitchen','Stove fire / Dead zones','Hazard volume','Player contact can trigger team recovery.'],['Kitchen','Dishwashing seesaw','Tilting bridge','Occupant positions change the plank angle.'],['Kitchen','Cold-storage plates','Two pressure plates','Unlock the vault route and remove its barrier.'],['Kitchen','Pantry symbol lock','Console','Solve symbol sums and enter a three-digit code.'],['Both','Narrow route / collision geometry','Traversal','Height differences and impacts threaten nest stability.'],['Outdoor','Platforms and wooden bridges','Traversal','Coordinate movement and jumps along the route.'],['Both','Death and finish areas','Round logic','Death restarts the pair; valid finish activates victory.']],[.65,1.2,1,3.4])
table(8,[['No','Scene / panel','Purpose','Main contents'],['1','Menu_Scene','Entry and local customisation','Main menu, name entry, room selection/lobby, shop and settings.'],['2','Credit','Credits','Team/author presentation.'],['3','Lobby panel','Room setup within Menu_Scene','Create, join code, roster, copy code and host Start.'],['4','Kitchen_Map','First gameplay map','Six zones, cooperative puzzles, checkpoints and victory trigger.'],['5','MainMap_Online','Second gameplay map','Outdoor platforms/bridges, coin objects, checkpoint and Win_Area.'],['6','GameWinPanel','In-scene victory UI','Next stage in Kitchen; replay in outdoor map; return-to-menu action.']],[.3,1.3,1.35,3.3])
table(9,[['Action','Input','Where','Behaviour'],['Move','WASD or arrow keys','Gameplay','Camera-relative movement; input is read only by the local owner.'],['Jump','Space','Gameplay','Grounded jump; close jump timestamps from both players can release the carried egg.'],['Camera','Mouse, using scene camera input','Gameplay','Controls the configured local camera.'],['Settings','Esc','Gameplay','Toggle settings; if tutorial is open, Esc closes that first. The online world is not paused.'],['Tutorial','H or F1','Gameplay','Toggle the tutorial panel.'],['Puzzle input','E; digits; R / Enter / Backspace','Nearby console','E enters/leaves; lights use 1–9 and R reset; symbol lock uses 0–9, Backspace and Enter.'],['Shop / equipment','UI buttons; Back or Esc','Menu','Preview/buy cosmetics; cycle owned equipment including no item.']],[.9,1.05,1,3.3])
table(10,[['No','Kitchen zone','Mechanic','Player task','Checkpoint'],['1','Prep Island','Linked lights','Use the console to turn all lights off and open the route.','1'],['2','Cookline Stoves','Pressure plates and fire','Coordinate the plate activation and avoid Dead zones.','2'],['3','Dishwashing Seesaw','Tilting plank','Balance the crossing using player positions.','3'],['4','Cold Storage Vault','Pressure-plate lock','Activate both plates to remove the barrier.','4'],['5','High-Rise Pantry','Symbol cipher','Read symbol sums and enter the code.','5'],['6','VIP Banquet and Patio','Final approach','Carry through the last section to the win trigger.','6'],['7','Victory panel','Next-stage action','Continue to MainMap_Online or return to menu.','—']],[.3,1.2,1.3,2.8,.65])
table(11,[['No','Outdoor element','Mechanic','Player task','Progress'],['1','Spawn area','Round preparation','Wait for both players, nest and egg to become ready.','Start'],['2','Raised platforms','Traversal','Move together without excessive height separation.','Route'],['3','Wooden bridges','Narrow crossing','Keep the nest clear of collision geometry.','Route'],['4','Checkpoint_Flag3','Shared checkpoint','Touch the checkpoint and establish a valid respawn centre.','Recovery'],['5','Dead zone','Team restart','Avoid falling; recover after a failed attempt.','Retry'],['6','Win_Area','Nest contact','Bring the nest to the finish and choose replay or menu.','Victory']],[.3,1.2,1.15,2.9,.7])
table(12,[['Mechanism','Type','Effect','Co-op response','Configuration'],['Linked lights','Puzzle','A press flips the selected lamp and orthogonal neighbours.','Coordinate entries until all nine lamps are off.','3×3 board; keys 1–9; R reset'],['Symbol cipher','Puzzle','Gate opens for the correct three-digit code.','Solve the circle/triangle/square pair sums.','Three sums stored on the puzzle'],['Kitchen plates','Puzzle','Removes a barrier and opens a gate.','Activate both plates under the grace rule.','4 s in current controllers'],['Seesaw','Traversal','Tilt depends on occupants’ lever arms.','Balance positions as both cross.','Component angle/sensitivity fields'],['Nest impact','Shared damage','Host chooses crack or egg ejection.','Avoid touching obstacles with the nest.','0.5 crack chance; 0.75 s cooldown'],['Excessive tilt','Egg release','Releases the egg from assisted carrying.','Avoid large vertical separation.','70° prefab release angle'],['Paired jumps','Egg release','Matched jump timestamps trigger release.','Plan timing; do not assume simultaneous jumping is safe.','0.694 s scene override'],['Dead zone','Round failure','Restarts the two-player round.','Use checkpoints to reduce lost progress.','Scene colliders / Dead convention'],['Finish area','Victory','Stops the round and presents the win panel.','Complete the current map’s finish condition.','Kitchen trigger; outdoor nest contact']],[1.05,.65,1.5,1.8,1.25])
table(13,[['No','Object','Implementation','Description'],['1','Host player','NetWorkPlayer prefab','Owner input/movement, nameplate and equipment.'],['2','Client player','Same prefab','Separate owner and equipment ID.'],['3','Nest and egg','NetworkNest / NestEggBody','Shared carrying; egg body created at runtime.'],['4','Checkpoint','OnlineCheckpoint','ID and safe respawn centre.'],['5','Finish','KitchenWinZoneTrigger / NestWinContact','Scene-specific victory condition.'],['6','Death zone','Dead colliders','Server-validated team recovery.'],['7','Puzzles','LinkedLights / SymbolCipher / Kitchen plates / Seesaw','Interactive scene mechanisms.'],['8','Coin','Coin + NetworkObject','Approved once; awards both wallets online.'],['9','Services','Session, round, currency and save modules','Connection, progression and persistence.']],[.3,1.05,2,2.9])
table(14,[['Element','Current implementation','Purpose'],['Creation','NestEggLifecycle.Create','Creates the egg object and NestEggBody.'],['Visual','EggVisual prefab or fallback','Fits the mesh to the runtime collision body.'],['Collider','CapsuleCollider','Height 3.06; radius 0.99 in runtime units.'],['Rigidbody','Mass 0.5; linear damping 0.05; angular damping 0.1','Server-side physics; mode changes between carrying and flight.'],['Authority','SetAuthority(server)','Remote bodies display replicated state.'],['Lifecycle','NestEggPhase','Waiting, Intact and Broken.'],['Damage','DamageCount 0–3','Tracks discrete damage stages.'],['Containment','NestEggContainment','Tests whether the egg remains inside the nest.'],['Assistance','NestEggCarryPhysics','Rolls the egg on the nest floor; releases on configured triggers.'],['Jump pairing','CooperativeJumpWindow','Pairs timestamps from the two different players.'],['Visual feedback','NestEggDamageVisual / EggDamageFeedback','Cracks, broken model, shake and screen flash.'],['Replication','NestEggFrame via NetworkNest','Egg is not a separate NetworkObject.'],['Recovery','RoundFailed → NetworkRoundManager','Restart at checkpoint/start after breakage.']],[1.05,2.3,2.9])
table(15,[['Parameter','Location','Current value','Meaning'],['Move speed','NetWorkPlayer prefab','10','Motor speed in project units/s.'],['Jump height','NetWorkPlayer prefab','6','Motor jump-height setting.'],['Damage count','NestEggBody','0 / 1 / 2 / 3','Third stage is Broken; release landing can break immediately.'],['Egg mass','NestEggBody','0.5','Rigidbody mass.'],['Egg height','NestEggBody','3.06','Runtime collider height.'],['Release tilt','Nest prefab','70°','Excess nest tilt ends assisted carrying.'],['Jump-pair window','Both gameplay scenes','0.694 s','Overrides the 0.2 s prefab value.'],['Jump release velocity','Both gameplay scenes','Up 0; side 2','Adds to inherited motion when paired jumps release the egg.'],['Nest impact crack chance','Nest prefab','0.5','Alternative outcome is ejection.'],['Nest impact cooldown','Nest prefab','0.75 s','Limits repeated nest-impact events.'],['Egg-break recovery delay','NetworkRoundManager default','1 s','Delay before attempting checkpoint restart.']],[1.4,1.5,.85,2.5])
table(16,[['From','To','Condition'],['Waiting','Intact','Prepared round resets and activates the egg.'],['Intact, damage 0','Intact, damage 1','Accepted damage event.'],['Intact, damage 1','Intact, damage 2','Next accepted damage event.'],['Intact, damage 2','Broken','Next accepted damage event.'],['Intact, any damage','Broken','Eligible landing after ReleaseCarry.'],['Intact','Broken','Safety fallback below Y = −50.'],['Broken','Intact','Checkpoint/start reset for the next round.'],['Active lifecycle','Waiting','Preparation/wait path stops active simulation.']],[1.3,1.3,3.65])
table(17,[['Component / module','Current use','Purpose'],['Duck model and rig','Duck_Wings_Expanded; carrying and hat anchors','Character presentation and attachment points.'],['CharacterController','Movement collision body','Grounding and collision flags.'],['KeyboardPlayerInput','WASD/arrows and Space','Local input only.'],['PlayerCharacterMotor','Gravity, jump and movement integration','Moves through CharacterController.'],['CarryDistanceConstraint','IPlayerMovementConstraint','Restricts separation while carrying.'],['OnlinePlayerStateMachine','Idle / Walk','Movement-state decisions.'],['PlayerAnimationPresenter / DuckNeckIK','Walk/side-walk and procedural rig adjustment','Animation follows movement and carrying pose.'],['NetworkObject / OwnerNetworkTransform','Owner-driven player movement','Replicates player pose.'],['NetworkPlayerIdentity / Equipment','Name and equipped-ID replication','Per-player identity and cosmetic visuals.']],[2.1,1.95,2.2])
table(18,[['No','Object','Presentation system','Current behaviour'],['1','Duck','PlayerAnimationPresenter','Walk/Walking and Side_Walk; restore base pose on idle.'],['2','Duck rig','DuckNeckIK','Neck/body/wing adjustment for carrying.'],['3','Egg','NestEggDamageVisual','Crack stages and broken-egg visual.'],['4','Damage feedback','EggDamageFeedback','Local camera shake and screen flash.'],['5','Puzzle gates','PuzzleGate / KitchenPuzzleGate','Translate or animate the gate into an open position.'],['6','Seesaw','SeesawBridge','Apply tilt and warning presentation.'],['7','Shop / loading','MenuShopTransition / CircularRevealGraphic','Camera framing, panel slide and circular transitions.']],[.3,1.1,2,2.85])

# Retain historical measurements, but correct their interpretation and provenance.
ts[19].cell(1,1).text='Current project: Unity 6000.3.15f1. The exact old capture revision was not recorded.'
ts[19].cell(2,1).text='URP; current package 17.3.0'
ts[19].cell(7,1).text='Historical screenshots from the supplied report; recapture the current Development Build.'
ts[20].cell(1,2).text='Project target for responsive PC play; not a measured guarantee.'
ts[20].cell(3,2).text='Limit recurring allocations during movement and shared-egg simulation.'
ts[20].cell(4,2).text='Initial investigation budget; judge changes using frame-time measurements.'
ts[21].cell(0,2).text='Evidence in original report'
for ri in range(1,5): ts[21].cell(ri,2).text='Historical capture / report'
ts[23].cell(0,6).text='Batches'
ts[24].cell(1,5).text='This frame’s script samples are small, but a selected frame cannot rule out CPU bottlenecks across the run. Separate active CPU work from presentation waits and inspect representative frames.'
ts[24].cell(2,5).text='DXGI.WaitOnSwapChain is a presentation wait, not a direct measurement of GPU execution time. Check GPU profiling, VSync/frame caps and render-thread timing before assigning the bottleneck.'
ts[25].cell(1,3).text='The capture contains five terrain draw events at that view. Recheck current scene/camera cost before deciding whether optimisation is needed.'
ts[25].cell(2,3).text='SRP Batcher events are visible. They reduce compatible draw setup costs; their presence does not mean six groups equal six total draw calls.'
ts[25].cell(3,3).text='One skybox draw is visible in the capture. Maintain only if current timing confirms acceptable cost.'

table(26,[['Step','Content','Highlight'],['1','Menu: shop preview, purchase and equipment cycling','Local customisation; no-item option'],['2','Host creates a room; second player joins by Relay code','Two-player roster and host Start'],['3','Kitchen loading, preparation and basic movement','Both ducks carry one nest and egg'],['4','Linked lights, plates, seesaw and symbol lock','Cooperative puzzle route'],['5','Demonstrate nest impact or paired-jump release','Cracks/ejection and egg breakage'],['6','Reach checkpoint, then trigger failure','Both players recover at the saved centre'],['7','Finish Kitchen and select Next Stage','Synchronised MainMap_Online load'],['8','Outdoor route, coin pickup and victory','Both wallets rewarded; win actions'],['9','Esc settings and one participant quitting','Settings and both machines returning to menu']],[.35,3.55,2.35])
table(27,[['Setting','Current project / deployment action'],['Target','Windows desktop build'],['Unity','6000.3.15f1'],['Backend','Confirm Windows backend in Build Profiles; this report does not claim a verified IL2CPP release.'],['Enabled scenes','Menu_Scene; Kitchen_Map; MainMap_Online; Credit'],['Rendering','URP 17.3.0; use tested project quality/audio settings.'],['Profiling','Enable Development Build for measurement; validate release separately.'],['Online services','Unity Services authentication and Relay; NGO with UnityTransport. Configure the linked Unity project/services for deployment.']],[1.3,4.95])
ts[28].cell(5,1).text='Stable Internet connection; verify under added latency/loss.'
ts[28].cell(5,2).text='Low-latency connection; no certified RTT limit yet.'

tests=[
('Create room','Host enters a name and creates a room.','Relay code and host roster entry appear.'),
('Join by code','Second machine enters the shared code.','Both names appear; room has two members.'),
('Invalid/full room','Attempt invalid code and a third participant.','Error/refusal without starting a match.'),
('Host start gate','Try with one player, then connect the second.','Only host starts; normal start requires two players.'),
('Load and spawn','Start the room.','Kitchen loads on both; each owns one duck; nest/egg prepare.'),
('Movement','Use WASD/arrows and Space on both machines.','Only the local owner controls its duck; both see movement.'),
('Carry and tilt','Walk, turn and change relative height.','Nest follows the pair; egg rolls; extreme tilt releases it.'),
('Paired jumps','Jump within the configured 0.694 s window.','Server pairs different players’ times and releases the egg.'),
('Nest impact','Contact an eligible obstacle with the nest.','Host chooses crack/ejection and both see the same result.'),
('Progressive damage','Produce successive accepted damage events.','Damage 1, then 2, then Broken; duplicate contact is filtered.'),
('Release landing','Release the egg and let it hit an eligible surface.','Egg breaks without requiring three prior hits.'),
('Checkpoint','Touch checkpoint with one player; then fail.','Valid centre is saved; both players and egg reset there.'),
('Death zone','Move one duck into a Dead zone.','Whole pair restarts; no score penalty is applied.'),
('Linked lights','At console: E, keys 1–9; test R.','Board follows cross-toggle rule; all-off opens gate.'),
('Kitchen plates','Activate both under the grace rule.','Barrier disappears and gate opens on both machines.'),
('Seesaw','Place players at different points on the plank.','Tilt/warning changes and is synchronised.'),
('Symbol code','Enter a wrong then correct three-digit code.','Wrong stays locked; correct opens the gate.'),
('Kitchen victory','Enter final trigger with player/nest; select Next Stage.','Win panel appears; both transition to MainMap_Online.'),
('Outdoor victory/replay','Bring nest into Win_Area; choose replay.','Win panel appears; replay resets to start and clears checkpoint.'),
('Coin reward','Collect the same online coin once.','Coin despawns; each wallet gains one; UI updates.'),
('Shop and save','Buy, relaunch, equip, preview another item and leave shop.','Owned item persists; saved equipment returns after preview.'),
('Per-player equipment','Choose different items, join and start.','Each duck has its own item on both machines.'),
('Settings / tutorial / quit','Use Esc and H/F1; change volume; quit from either peer.','Correct panels open; audio saves; both return through circular transition.'),
('Latency and release build','Test added delay/loss and clean-PC release; profile both maps.','Record actual failures, timings and performance against targets.')]
table(29,[['ID','Test case','Steps','Expected result','Result']]+[[f'TC-{i:02}',*r,'Pending'] for i,r in enumerate(tests,1)],[.5,1.05,1.65,2.5,.55])
table(30,[['No','Open issue / validation item','Impact','Status'],['1','Purchase service relies on UI to block repeat purchases; currency and ownership are local.','Validation / save integrity','Review before release'],['2','Current build still needs two-machine regression and fresh performance captures.','Runtime / performance','Pending']],[.35,3.35,1.35,1.2])
table(31,[['Objective','Current implementation status'],['Two players carry a shared nest and egg','Source and gameplay scenes contain the carrying, damage and recovery systems.'],['Egg lifecycle and crack feedback','Waiting/Intact/Broken plus DamageCount; no boiling or continuous stress model.'],['Two gameplay maps','Kitchen_Map and MainMap_Online are enabled in the build.'],['Online session','NGO + Unity Transport + Relay; create/join, host start and disconnect return.'],['Coins and cosmetics','Shared online pickup rewards; local purchase/save; per-player equipped-ID replication.'],['Profiling and tests','Historical captures retained; current regression/benchmark checklist is pending.'],['Deployment','Build steps defined; clean-PC and final multiplayer release checks remain to be completed.']],[2.3,3.95])

# Remove obsolete figure-list screenshots and the outdated ISP example, keeping
# relevant code captures and historical performance screenshots in their sections.
replace(18,'The main-menu and Relay screenshots retained in Part 1 document the earlier interface. The revised flow and lifecycle descriptions reflect the current implementation.')
replace(19,'')
replace(21,'Tables are numbered in reading order. The contents page and page numbers are refreshed in Word.')

def insert_after(p,text,style='normal'):
    el=OxmlElement('w:p'); p._p.addnext(el)
    result=Paragraph(el,p._parent); result.style=d.styles[style]; result.add_run(text)
    return result

# Keep the online section with gameplay architecture, before performance analysis.
performance_start=ps[175]._p
for idx in range(178,184):
    performance_start.addprevious(ps[idx]._p)
ps[178].text='2.4 Online Session Flow'
ps[175].text='2.6 Performance Analysis'
for idx in [184,187,190,199,202,206,226]:
    ps[idx].text=ps[idx].text.replace('2.4.','2.6.')
    ps[idx].style=d.styles['Heading 3']
p=ps[183]
for text,style in [
('2.5 Shop and Equipment','Heading 2'),
('The shop uses the menu duck as the preview character. MenuShopTransition moves the camera closer and slides the item panel in from the right. ShopUI selects a slot and ShopPreview asks CharacterEquipment to display the corresponding prefab at HatAnchor. Previewing does not save a new equipped item.','normal'),
('Buy calls ShopPurchaseService, which requests SpendCoin, appends the item ID to the owned list and saves it. SlotItem marks owned items and disables their button. CurrencyManager raises OnCoinChanged so subscribed coin displays refresh immediately.','normal'),
('Outside the shop, PlayerEquipment cycles through owned IDs and an empty ID for no equipment. The chosen ID is saved. Closing the shop restores this saved selection. When an online player spawns, only its owner submits the saved ID; the server distributes that player’s ID and each machine creates the matching visual locally.','normal'),
('SaveSystem stores coins using PlayerPrefs.SetInt and stores purchased IDs/equipped ID as JSON strings through JsonUtility and PlayerPrefs. These are local settings, not cloud/database records. The current network equipment check validates a known item ID, not a server-maintained purchase ledger.','normal')]:
    p=insert_after(p,text,style)

# Captions: preserve the original caption order but remove gaps left by old draft tables.
caption_count=0
for p in d.paragraphs:
    if p.text.startswith('Table '):
        caption_count+=1
        title=p.text.split(':',1)[1] if ':' in p.text else ''
        p.text=f'Table {caption_count}:{title}'
        p.paragraph_format.keep_with_next=True
    if p.text.startswith('Figure '): p.paragraph_format.keep_with_next=False

# Preserve the report hierarchy; the old document used plain paragraphs for Parts 4–5.
for idx in [268,293]: ps[idx].style=d.styles['Heading 1']
for idx in [247,261,269,285]: ps[idx].style=d.styles['Heading 2']
for idx in [248,251,258,265,270,273,277,281]: ps[idx].style=d.styles['Heading 3']
ps[165].style=d.styles['normal']
ps[17].text='FIGURES AND EVIDENCE'
ps[20].text='TABLES'

# Delete stale TOC containers/fields. A new automatic TOC is inserted in Word.
for sdt in list(d._element.xpath('//w:sdt')):
    if sdt.findall('.//'+qn('w:instrText')):
        sdt.getparent().remove(sdt)
for p in d.paragraphs:
    if p._p.xpath('.//w:instrText') and p not in [ps[16]]:
        for field in p._p.xpath('.//w:fldSimple'): field.getparent().remove(field)
ps[16].clear(); ps[16].add_run('[[AUTO_TOC]]')

# Add concise review comments at the major corrected claims.
comments={
26:'Replaced the direct wing-grip/chick story with the implemented duck-and-nest carrying arrangement. Sources: NetWorkPlayer prefab, NetworkNest and NestCarryPose.',
60:'Scene names checked against ProjectSettings/EditorBuildSettings.asset. Lobby, shop and victory are panels, not separate enabled build scenes.',
79:'Victory and checkpoint conditions checked in KitchenWinZoneTrigger, NetworkNest, NestWinContact and NetworkRoundManager. The old simultaneous three-object finish rule was not implemented.',
81:'Coin.RewardClientsRpc targets ClientsAndHost and awards each wallet one coin. Removed unimplemented seeds, scoring and stars.',
124:'Replaced the proposed continuous stress formulas with NestEggBody damage events and NetworkNest impact outcomes. Current working-tree changes were included in this review.',
132:'Scene overrides were checked as well as prefab defaults. Both scenes currently override the cooperative-jump window to 0.694 seconds.',
163:'Removed the older ISpawnable/SpawnPlayer screenshots and used current narrow interfaces from coin, movement and puzzle modules.',
179:'Networking implementation uses Netcode for GameObjects, Unity Transport and Relay. No Fusion, quick-match or fixed four-character code flow is implemented.',
195:'Historical profiler numbers/images retained. They have not been remeasured against the current project. Interpretation of presentation waits and batching was corrected.',
262:'Test cases rewritten for actual mechanisms. Pending is intentional: source review is not an executed two-machine test.',
14:'Original member information is preserved. The supplied report lists Dương as HE19150 in the member table and HE191504 in the task table; the team should confirm the correct student ID.'}
if hasattr(d,'add_comment'):
    for idx,text in comments.items():
        if ps[idx].runs: d.add_comment(ps[idx].runs,text=text,author='Project review',initials='PR')

d.add_heading('Appendix — Implementation Sources',1)
d.add_paragraph('The revised mechanics correspond to the current project files below. These references make the report traceable without presenting a source review as a runtime test.')
sources=[
('Engine and build scenes','ProjectSettings/ProjectVersion.txt; ProjectSettings/EditorBuildSettings.asset; Packages/manifest.json'),
('Maps and overrides','Assets/Scenes/Kitchen_Map.unity; Assets/Scenes/MainMap_Online.unity; Assets/Prefabs/Nest.prefab; Assets/Prefabs/NetWorkPlayer.prefab'),
('Input and movement','Assets/Scripts/Player/Input/KeyboardPlayerInput.cs; Movement/PlayerCharacterMotor.cs; Movement/OnlinePlayerStateMachine.cs; Camera/LocalPlayerCamera.cs'),
('Shared carrying and egg','Assets/Scripts/NestEggPhysics/Nest/Networking/NetworkNest.cs; Egg/Physics/NestEggBody.cs; NestEggCarryPhysics.cs; CooperativeJumpWindow.cs; Egg/Lifecycle/NestEggLifecycle.cs'),
('Victory and recovery','Assets/Scripts/RoundManagement/NetworkRoundManager.cs; OnlineCheckpoint.cs; KitchenWinZoneTrigger.cs; Assets/Quang/Scripts/OnlineWinButtons.cs'),
('Kitchen puzzles','Assets/Scripts/KitchenPuzzle/; Assets/_Hoang/Scripts/Puzzles/LinkedLightsBoard.cs; SymbolCipherLock.cs; SeesawBridge.cs and their controllers/consoles'),
('Sessions','Assets/Scripts/OnlineStartup/Services/RelayRoomService.cs; Session/NetworkGameStarter.cs; NetworkSessionRuntime.cs; Lobby/RoomLobbyView.cs'),
('Shop and persistence','Assets/Scripts/Shop/; Assets/Scripts/Player/Inventory/; Assets/Scripts/Coins/Persistence/SaveSystem.cs; Assets/Scripts/Player/Networking/NetworkPlayerEquipment.cs'),
('Coins and UI events','Assets/Scripts/Coins/Collection/Coin.cs; Wallet/CurrencyManager.cs; Presentation/CoinUI.cs'),
('Settings and transitions','Assets/Quang/Scripts/InGameSettingsShortcut.cs; Assets/Scripts/Menu/SettingPanelController.cs; MenuShopTransition.cs; Assets/Scripts/GameLoading/ReturnToMenuTransition.cs'),
('Existing puzzle tests','Assets/_Hoang/Tests/Editor/LinkedLightsBoardTests.cs; SymbolCipherLockTests.cs; prefab-validation scripts')]
for topic,path in sources:
    p=d.add_paragraph(); p.add_run(topic+': ').bold=True; p.add_run(path)

# Keep pagination fluid and preserve photos/code screenshots at their original size.
for name in ['Heading 1','Heading 2','Heading 3','Title']:
    d.styles[name].font.color.rgb=RGBColor(0,0,0)
for p in d.paragraphs:
    if p.text and p.style.name=='normal':
        p.paragraph_format.widow_control=True
        p.paragraph_format.space_after=Pt(6)
    if p.style.name.startswith('Heading'):
        p.paragraph_format.keep_with_next=True
for tbl in d.tables:
    for row in tbl.rows:
        for cell in row.cells:
            for p in cell.paragraphs:
                p.paragraph_format.keep_with_next=False
    if tbl.rows:
        hdr=OxmlElement('w:tblHeader'); tbl.rows[0]._tr.get_or_add_trPr().append(hdr)

# Repair old draft formatting: blank forced pages, placeholder shading, oversized
# fixed table rows and inherited bold body runs otherwise survive text edits.
for idx in range(17,23):
    el=ps[idx]._p
    if el.getparent() is not None: el.getparent().remove(el)
for p in list(d.paragraphs):
    if not p.text.strip() and not p._p.xpath('.//w:drawing') and not p._p.xpath('.//w:sectPr'):
        p._p.getparent().remove(p._p)
        continue
    prop=p._p.get_or_add_pPr()
    for tag in ['w:pBdr','w:shd','w:framePr']:
        for el in list(prop.findall(qn(tag))): prop.remove(el)
    if p.style.name=='normal' and p.text and not p.text.startswith(('Table ','Figure ')):
        p.paragraph_format.space_before=Pt(0)
        p.paragraph_format.space_after=Pt(6)
        p.paragraph_format.line_spacing=1.08
        if p not in ps[:16]:
            for run in p.runs:
                run.bold=False; run.italic=False
                run.font.size=Pt(11); run.font.color.rgb=RGBColor(0,0,0)
    if p.text.startswith('Figure 8:'): p.text=p.text.replace('Figure 8:','Figure 4:')
for idx in [7,10,15,23,90,232,268,293]:
    ps[idx].paragraph_format.page_break_before=True
for p in d.paragraphs:
    if p.text=='Appendix — Implementation Sources': p.paragraph_format.page_break_before=True

# Preserve capture numbers while shortening oversized interpretation tables.
table(24,[['Scenario','Frame','CPU time','Observation','Main thread','Interpretation'],
['Menu / Lobby','1742 / 2051','Peak >66 ms','Historical graph contains repeated high frame-time spikes.','Selected samples: skinned meshes 0.26 ms; NetworkEarlyUpdate 0.26 ms; scripts 0.16 ms.','One selected frame cannot rule out a CPU bottleneck across the run. Separate work from waits.'],
['Gameplay','48 / 1447','33.63 ms','A large presentation-wait block is visible.','DXGI.WaitOnSwapChain 19.26 ms; physics 2.30 ms; scripts 2.23 ms.','Presentation wait is not GPU execution time. Check GPU timing, VSync/caps and the render thread.']],
[.65,.6,.7,1.25,1.45,1.6])
table(25,[['Scenario','Draw event','Historical observation','Follow-up'],
['Gameplay','Terrain','Five terrain draw events visible at the captured view.','Remeasure current scene/camera cost before optimising.'],
['Gameplay','Opaque SRP batches','SRP Batcher events are visible.','Check compatible setup costs; batch groups are not the total draw-call count.'],
['Gameplay','Skybox','One skybox draw visible in this capture.','Retain settings only after checking current timing.']], [.75,1.1,2.1,2.3])
for ti,tbl in enumerate(ts):
    for ri,row in enumerate(tbl.rows):
        prop=row._tr.get_or_add_trPr()
        for el in list(prop.findall(qn('w:trHeight'))): prop.remove(el)
        if not prop.findall(qn('w:cantSplit')): prop.append(OxmlElement('w:cantSplit'))
        for cell in row.cells:
            for p in cell.paragraphs:
                p.paragraph_format.keep_with_next=False
                p.paragraph_format.space_before=Pt(2)
                p.paragraph_format.space_after=Pt(3)
                p.paragraph_format.line_spacing=1.0
            if ri==0:
                shade=OxmlElement('w:shd'); shade.set(qn('w:fill'),'FFE08A')
                cell._tc.get_or_add_tcPr().append(shade)
    if tbl.rows[0].cells[0].text in ['No','Step']: tbl.rows[0].cells[0].text='#'
    for row in tbl.rows:
        for cell in row.cells:
            pr=cell._tc.get_or_add_tcPr()
            mar=OxmlElement('w:tcMar')
            for side in ['left','right']:
                edge=OxmlElement('w:'+side); edge.set(qn('w:w'),'70'); edge.set(qn('w:type'),'dxa'); mar.append(edge)
            pr.append(mar)
    if ti==30:
        for row in tbl.rows[:-1]:
            for cell in row.cells:
                for p in cell.paragraphs: p.paragraph_format.keep_with_next=True
for row in ts[29].rows:
    for ci,w in enumerate([.5,1.05,1.55,2.45,.7]): row.cells[ci].width=Inches(w)
for col,w in zip(ts[29].columns,[.5,1.05,1.55,2.45,.7]): col.width=Inches(w)

captions=['Team members','Nine-week development plan','Team responsibilities',
'Game overview','Player roles','Carried and released egg','Egg damage stages',
'Hazards and puzzles','Scenes and panels','Player controls','Kitchen route',
'Outdoor route','Mechanism configuration','Main game objects','Runtime egg components',
'Current carrying and damage parameters','Egg state transitions','Player components',
'Animation and presentation','Test environment','Performance targets','Profiling tools',
'Historical capture scenarios','Historical measurement results','CPU and presentation-wait analysis',
'Frame Debugger observations','Demo storyboard','Build configuration','Target system requirements',
'Test checklist','Open issues and validation','Objectives and implementation']
for p in list(d.paragraphs):
    if p.text.startswith('Table '): p._p.getparent().remove(p._p)
for idx,tbl in enumerate(ts):
    el=OxmlElement('w:p'); tbl._tbl.addprevious(el)
    p=Paragraph(el,d._body); p.style=d.styles['normal']
    p.add_run(f'Table {idx+1}: {captions[idx]}')
    p.paragraph_format.keep_with_next=True
    p.paragraph_format.space_before=Pt(6); p.paragraph_format.space_after=Pt(4)
    for run in p.runs: run.font.size=Pt(10)
settings=d.settings.element
update=OxmlElement('w:updateFields'); update.set(qn('w:val'),'true'); settings.append(update)
d.core_properties.title='Egg Press 3D — Updated Project Report'
d.core_properties.subject='Current gameplay, architecture, level flow and test plan'
d.save(OUT)
(ROOT/'qa'/'changes.json').write_text(json.dumps(changes,ensure_ascii=False,indent=2),encoding='utf-8')
print(OUT)
print('Changed paragraphs/tables:',len(changes),'Comments:',len(comments))
