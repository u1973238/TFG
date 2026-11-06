# Revisió del codi respecte a la memòria del TFG

**Projecte:** *Desenvolupament d'un joc de plataformes 3D inspirat en el moviment d'una aranya* — Josep Serra Mas
**Data de la revisió:** 7 d'octubre de 2026
**Què s'ha revisat:** els 23 scripts d'`Assets/Scripts`, l'escena `Assets/Scenes/Joc.unity` (l'única del build), els prefabs d'`Assets/Prefabs`, les 6 missions (`MissionData`), `GameInput.inputactions`, la configuració del projecte i el `Build.zip`. Tot s'ha comparat amb la memòria `TFG_JosepSerraMas (1).pdf`.

Llegenda: ✅ coincideix · ⚠️ coincideix en part · ❌ no coincideix

---

## 1. Resum

- Les mecàniques principals **estan implementades i funcionen**: locomoció, escalada, swing, connectar, NPCs, missions, pausa i victòria. En la majoria de punts el codi fa el que diu la memòria.
- S'han trobat **9 errors** que feien que algun repte no es comportés com diu la memòria, o que el joc es pogués quedar bloquejat sense poder acabar-lo. **Tots 9 ja estan corregits** al codi i a l'escena. L'últim, els botons de la pantalla de victòria, s'ha resolt amb els menús nous (secció 2.1). Detall a la secció 4.
- En una segona ronda s'ha fet que el joc **vagi més fluid** i s'hi han **afegit funcionalitats** (secció 5):
  - Ara existeixen el **Grappling** i el **comandament d'Xbox**, que la memòria ja descrivia.
  - Hi ha **previsualització dels ponts**, **escalada ràpida**, **avisos de missió**, un **efecte en recollir objectes** i una **xarxa de seguretat** si l'aranya cau fora del mapa.
  - L'escalada és **més enganxosa**: l'aranya ja no es desenganxa dels sostres en moure's, però saltant continua desenganxant-se a l'instant (M9).
- Els **menús** ja estan muntats a l'escena: menú principal, pausa, controls, opcions amb pestanyes i victòria, tots amb el mateix estil (secció 2.1).
- Encara hi ha coses **que la memòria descriu però el joc no fa**, com el lliurament de missions o el circuit aeri en ordre. A la secció 6 tens text alternatiu.
- Alguns apartats del capítol 6 descriuen **una versió antiga del codi**. El cas més clar és l'escalada: el codi real és **més avançat** del que explica la memòria. A la secció 7 tens paràgrafs nous per substituir-los.

---

## 2. Què has de fer tu a Unity

Els canvis de codi i d'escena ja estan fets.

> **En obrir el projecte**, Unity tornarà a generar `GameInput.cs` a partir de `GameInput.inputactions`. És normal: s'hi han afegit l'acció *Grapple* i els botons del comandament. Els camps nous dels scripts sortiran a l'Inspector amb els valors per defecte.

### 2.1 Menús (ja fet)

Els menús ja estan muntats a `Joc.unity`, dins de l'objecte `UI_Menus`. Els ha creat l'eina **Tools > TFG > Construir menús**, i tots tenen el mateix estil:

- **Menú principal:** Jugar, Controls, Opcions i Sortir. Mentre es veu, el joc està aturat i el HUD (missions i punt de mira) està amagat.
- **Pausa** (Esc, P o el botó Menú del comandament): Continuar, Controls, Opcions i Menú principal.
- **Controls:** una taula amb les tecles i els botons del comandament.
- **Opcions**, amb tres pestanyes. Amb el comandament, LB i RB canvien de pestanya.
  - **So:** volum general, música i efectes de so.
  - **Càmera:** sensibilitat del ratolí i del comandament, i invertir l'eix vertical.
  - **Pantalla:** pantalla completa, qualitat gràfica (Baixa o Alta) i sincronització vertical.
- **Victòria:** el temps de la partida, Menú principal i Sortir. Això corregeix l'error A4.

Les opcions es guarden entre partides. Esc i el botó B del comandament fan de «Tornar».

Els panells antics de pausa i victòria (dins d'`UI_Pantalla_Sempre`) estan desactivats, però no s'han esborrat.

El menú és dins de la mateixa escena. Per tant, «Menú principal» (a la pausa i a la victòria) torna a carregar el nivell **i mostra el menú principal**. Això fa que el «Return to menu» de la memòria (5.3.2) sigui cert.

**Per canviar-los:** el títol del joc i els textos són a [MenuBuilder.cs](Assets/Editor/MenuBuilder.cs) (`GameTitle`, `GameSubtitle`, la taula de controls...). Quan els canviïs, torna a executar *Tools > TFG > Construir menús* i desa l'escena: l'eina esborra `UI_Menus` i el torna a fer. Si toques els menús a mà a l'Inspector, recorda que tornar a executar l'eina desfà aquests canvis.

**Qualitat gràfica:** perquè hi hagi on triar, el nivell *Mobile* de *Project Settings > Quality* ara també està activat per a PC (Standalone). Al menú surt com a «Baixa». El nivell *PC* surt com a «Alta» i continua sent el nivell per defecte.

### 2.2 Opcional

- **Saltar damunt dels ponts.** Si vols que l'aranya pugui saltar quan camina per sobre d'un pont de teranyina, ves a *Player > SpiderLocomotion > Walkable Layers* i afegeix-hi la capa `Climbing` (vegeu C3).
- **Component sobrant.** Pots treure el component `PlayerInput` del Player: apunta a un arxiu d'accions que ja no existeix i no s'utilitza.

---

## 3. Taula de verificació per mecànica

| Mecànica (apartat de la memòria) | Implementació | Estat |
|---|---|---|
| Càmera orbital en 3a persona (5.1.2, 6.2.1) | [ThirdPersonCamera.cs:43](Assets/Scripts/ThirdPersonCamera.cs#L43) | ✅ — l'oclusió estava desactivada, corregit (A8); més suau (M3) |
| Lector d'entrades amb ScriptableObject (6.2, 6.2.2.1) | [InputReader.cs](Assets/Scripts/InputReader.cs) | ✅ teclat, ratolí i ara també comandament (N2); l'escalada no l'usava, corregit (A9) |
| Córrer (5.2.3, 6.2.2.2) | [SpiderLocomotion.cs:70](Assets/Scripts/Movement/SpiderLocomotion.cs#L70) | ✅ |
| Saltar (5.2.3, 6.2.2.2) | [SpiderLocomotion.cs:112](Assets/Scripts/Movement/SpiderLocomotion.cs#L112) | ✅ — ara amb coyote time i buffer (M4) |
| Escalar / locomoció omnidireccional (5.2.3, 6.2.2.4) | [SpiderClimbing.cs:106](Assets/Scripts/Movement/SpiderClimbing.cs#L106) | ✅ el codi · ❌ el text de 6.2.2.4 és antic (C2) |
| Salt de paret | [SpiderClimbing.cs:292](Assets/Scripts/Movement/SpiderClimbing.cs#L292) | ✅ existeix però no surt a la memòria (C2); doble impuls corregit (A6); temps d'espera (M5) |
| Swing / balanceig (5.2.3, 6.2.2.3) | [SpiderSwinging.cs:85](Assets/Scripts/Movement/SpiderSwinging.cs#L85) | ✅ el codi · ⚠️ el text (C1) |
| Grappling (1.3, 5.2.1, 5.2.4, 5.3.3) | [SpiderSwinging.cs:113](Assets/Scripts/Movement/SpiderSwinging.cs#L113) | ✅ **afegit a la segona ronda** (N1) |
| Connectar / ponts de teranyina (5.2.3, 6.2.2.5) | [WebConnector.cs:87](Assets/Scripts/Movement/WebConnector.cs#L87) | ✅ els ponts, ara amb previsualització (N3) · ⚠️ no enllaça objectes físics (B5) · ⚠️ capa (C3) |
| Puzle 1: palanca (5.2.2, 5.2.4, 6.5.1) | [LeverDoorSystem.cs:30](Assets/Scripts/Quest/QuestImplementation/LeverDoorSystem.cs#L30) | ✅ funciona · ⚠️ el text (B6, C5) |
| Puzle 2: placa de pressió (5.2.2, 6.5.2) | [PressurePlate.cs:25](Assets/Scripts/Quest/QuestImplementation/PressurePlate.cs#L25) + Swing a la roca | ✅ — el fil no seguia la roca, corregit (A7) |
| Puzle 3: col·leccionables (5.2.2, 6.5.3) | [MissionCollectible.cs:21](Assets/Scripts/Quest/QuestImplementation/MissionCollectible.cs#L21) | ✅ — amb efecte en recollir (N6) · ⚠️ el text (C6) |
| Puzle 4: xarxa de salvament (5.2.2, 6.5.4) | [PlatformLinkTrigger.cs](Assets/Scripts/Quest/QuestImplementation/PlatformLinkTrigger.cs) | ✅ — es podia bloquejar, corregit (A2) · ⚠️ el text (C7) |
| Repte 1: cursa contrarellotge (5.2.2, 6.5.5) | [TimeTrialChallenge.cs](Assets/Scripts/Quest/QuestImplementation/TimeTrialChallenge.cs), [RaceNPC.cs](Assets/Scripts/Quest/RaceNPC.cs) | ✅ — el temps límit no funcionava, corregit (A1); ara també hi ha escalada ràpida (N4) · ⚠️ el text (C8) |
| Repte 2: circuit aeri (5.2.2, 6.5.6) | `MissionCollectible` a les 5 anelles `AroAeri` | ⚠️ no cal fer-lo a l'aire ni en ordre (B4) |
| NPCs i diàlegs (5.2.6, 6.4) | [MissionNPC.cs:38](Assets/Scripts/Quest/MissionNPC.cs#L38) | ✅ · ❌ no hi ha «lliurament» (B3) |
| Menú principal (5.3.2, 6.3.1) | [MainMenu.cs](Assets/Scripts/UI/MainMenu.cs) | ✅ muntat a l'escena, amb Controls i Opcions (secció 2.1) |
| Menú de pausa (5.3.2, 6.3.2) | [PauseMenu.cs:40](Assets/Scripts/UI/PauseMenu.cs#L40) | ✅ — corregit (A3); ara amb Controls i Opcions (B8) |
| Menú final (5.3.2, 6.3.3) | [MissionManager.cs:94](Assets/Scripts/Quest/MissionManager.cs#L94), [MenuVictoria.cs](Assets/Scripts/UI/MenuVictoria.cs) | ✅ — no tenia botons, corregit (A4, secció 2.1) |
| Crosshair (5.3.3, 6.3.4.1) | [DynamicCrosshair.cs:27](Assets/Scripts/UI/DynamicCrosshair.cs#L27) | ✅ — ara indica també el pont a mig fer (N3) · ⚠️ (B7) |
| Quest Tracker (5.3.3, 6.3.4.2) | [MissionManager.cs:111](Assets/Scripts/Quest/MissionManager.cs#L111) | ✅ — línia duplicada, corregit (A5); amb avisos (N5) · ⚠️ el text (C4) |
| Manual de controls (12.2) | `GameInput.inputactions` | ✅ les tecles · ⚠️ faltes i tecles noves (C10) |

---

## 4. Errors trobats i corregits

### A1. Cursa contrarellotge: el temps límit no tenia cap efecte

**La memòria diu (5.2.2, Repte 1):** «Arribar a la línia de meta [...] abans que el cronòmetre s'esgoti».

**Problema:**
- A l'objecte `Meta`, el `TimeTrialChallenge` tenia l'id `cursa_vertical` (en minúscula), però la missió es diu `Cursa_vertical`.
- `MissionManager.ProgressMission` compara els ids amb `==`, que distingeix majúscules. Per tant, el progrés no s'acceptava mai.
- En realitat, la cursa la completava un `MissionCollectible` que també era a `Meta`, i aquest **no mira el cronòmetre**. Es podia arribar al cim després del «Temps esgotat!» i la missió es completava igualment.

**Correcció:**
- `Joc.unity`: s'ha tret el `MissionCollectible` de `Meta` i l'id ara és `Cursa_vertical`.
- [TimeTrialChallenge.cs:6](Assets/Scripts/Quest/QuestImplementation/TimeTrialChallenge.cs#L6): el valor per defecte ara és `Cursa_vertical`.
- [TimeTrialChallenge.cs:74](Assets/Scripts/Quest/QuestImplementation/TimeTrialChallenge.cs#L74): la cursa només es marca com a completada si el `MissionManager` accepta el progrés.

### A2. Xarxa de salvament: el joc es podia quedar bloquejat

**Problema:**
- `PlatformLinkTrigger` posava `isConnected = true` abans de saber si la missió estava activa.
- Si el jugador feia el pont **abans** de parlar amb l'NPC, el progrés es rebutjava i no es tornava a intentar mai.
- La missió ja no es podia completar. Com que la victòria demana totes les missions, **la pantalla final no sortia mai**.

**Correcció ([PlatformLinkTrigger.cs](Assets/Scripts/Quest/QuestImplementation/PlatformLinkTrigger.cs)):**
- El Trigger només recorda que hi ha un pont (`bridgeDetected`).
- A `Update` es torna a provar `ProgressMission` fins que el gestor l'accepta.
- Si el pont ja existeix quan es parla amb l'NPC, la missió es completa a l'instant.

### A3. La pausa s'obria per sobre de la pantalla de victòria

**Problema:** amb la pantalla de victòria oberta (temps aturat), prémer Esc obria la pausa. Després, «Resume» tornava a posar `Time.timeScale = 1` i bloquejava el cursor, amb el panell de victòria encara a la pantalla.

**Correcció:** [PauseMenu.cs:43](Assets/Scripts/UI/PauseMenu.cs#L43). Si el joc ja està aturat per una altra pantalla (la victòria o el menú principal), la tecla de pausa s'ignora.

### A4. La pantalla de victòria no tenia botons

**La memòria diu (6.3.3):** «ofereix al jugador les opcions de reiniciar la partida o sortir de l'aplicació».

**Problema:** `MenuVictoria.ReiniciarJoc()` i `SortirDelJoc()` existeixen, però el panell `MenuVictoria` de l'escena només té un `Text`. Cap botó crida aquestes funcions.

**Correcció:** la pantalla de victòria nova (secció 2.1) té els botons «Menú principal» (`ReiniciarJoc`) i «Sortir» (`SortirDelJoc`), i mostra el temps de la partida.

### A5. La cursa sortia dues vegades al Quest Tracker

**Problema:** `Cursa_vertical` era dues vegades a la llista `allMissions` del `MissionManager`.

**Correcció:** a `Joc.unity` s'ha eliminat l'entrada duplicada. Ara hi ha 6 missions.

### A6. El salt de paret podia sumar dos impulsos

**Problema:**
- Mentre s'escala, `SpiderLocomotion` està desactivat, però continua subscrit a `jumpEvent`. Desactivar un component no anul·la les subscripcions.
- A més, el seu `isGrounded` es queda amb el valor que tenia abans d'enganxar-se a la paret.
- Per això, el primer salt de paret també aplicava l'impuls del salt normal.

**Correcció:** [SpiderLocomotion.cs:73](Assets/Scripts/Movement/SpiderLocomotion.cs#L73). La tecla de salt només es registra si el component està actiu. A la segona ronda, el salt passa a fer-se a `TryJump` (M4).

### A7. El fil no seguia la roca en arrossegar-la

**La memòria diu (5.2.2, Puzle 2):** «enganxar teranyines a l'objecte i estirar-lo».

**Problema:** `DrawRope` dibuixava sempre fins al punt del món on havia impactat el raig. En moure la roca, el fil es quedava penjant a l'aire.

**Correcció:**
- [SpiderSwinging.cs:197](Assets/Scripts/Movement/SpiderSwinging.cs#L197): si la junta està connectada a un `Rigidbody`, l'extrem del fil segueix l'objecte.
- [SpiderSwinging.cs:138](Assets/Scripts/Movement/SpiderSwinging.cs#L138): l'àncora local ara es calcula respecte al `Rigidbody`.

### A8. L'oclusió de la càmera estava desactivada

**La memòria diu (6.2.1):** «Prevenció de Traspàs de Murs».

**Problema:** a `Joc.unity`, el camp `collisionLayers` de la càmera era `Nothing`. Per això el `Physics.Raycast` de `CheckCameraCollision` no tocava mai res.

**Correcció:** a `Joc.unity`, `collisionLayers` ara és `Walkable` + `Climbing`. A la segona ronda, el raig passa a ser una esfera (M3).
- **Prova-ho.** Si la càmera s'acosta massa sovint (per exemple, amb arbres o ponts), pots ajustar-ho a l'Inspector.

### A9. L'escalada llegia el teclat pel sistema antic

**La memòria diu (6.2.2.1):** cap script consulta el teclat directament; tot passa per l'`InputReader`.

**Problema:** `SpiderClimbing` feia servir `Input.GetAxisRaw`, que és de l'Input Manager antic. Només funcionava perquè el projecte té *Active Input Handling = Both*.

**Correcció:** [SpiderClimbing.cs:228](Assets/Scripts/Movement/SpiderClimbing.cs#L228). L'escalada ara rep el moviment de `inputReader.moveEvent`, com la resta d'scripts. Per això també funciona amb el comandament.

### Altres canvis de la primera ronda

- **[MainMenu.cs](Assets/Scripts/UI/MainMenu.cs):**
  - S'hi ha afegit el panell de controls (`ShowControls` / `HideControls`).
  - S'hi ha afegit `[DefaultExecutionOrder(100)]` perquè el seu `Start` s'executi després del de `ThirdPersonCamera`, que bloqueja el cursor. Així el cursor queda lliure al menú.
- **[ThirdPersonCamera.cs:49](Assets/Scripts/ThirdPersonCamera.cs#L49):** amb el joc aturat (menú, pausa o victòria), ja no s'acumula el moviment del ratolí. Abans, la càmera «saltava» en reprendre el joc.

---

## 5. Millores afegides (segona ronda)

Totes les millores tenen els valors a l'Inspector. Si alguna no t'agrada, a la secció 5.4 hi ha com ajustar-la o desactivar-la.

**Comprovació de compilació:** `dotnet build Assembly-CSharp.csproj` dona **0 errors i 0 avisos**.

### 5.1 Més fluïdesa

#### M1. Interpolació del Rigidbody
- **Abans:** la física s'executava a 50 Hz (*Fixed Timestep* 0,02) i la pantalla a 60 Hz o més. Sense interpolació, l'aranya i la càmera que la segueix es movien a petites batzegades.
- **Ara:** [SpiderLocomotion.cs:43](Assets/Scripts/Movement/SpiderLocomotion.cs#L43) activa `RigidbodyInterpolation.Interpolate`.
- Perquè la interpolació funcioni bé, les rotacions es fan amb `rb.MoveRotation` / `rb.rotation` en lloc de modificar el `Transform`.
- `SpiderClimbing` llegeix la posició i la rotació del `Rigidbody`, perquè el `Transform` interpolat va un pas de física endarrerit.

#### M2. VSync activat
- *Project Settings > Quality*, nivell **PC** (el que fan servir els builds de Windows): `VSync Count` = *Every V Blank*.
- Evita el *tearing* (la imatge partida) i fa que els fotogrames arribin a un ritme regular.

#### M3. Càmera més suau
[ThirdPersonCamera.cs:71](Assets/Scripts/ThirdPersonCamera.cs#L71) i [:85](Assets/Scripts/ThirdPersonCamera.cs#L85):
- **Suavitzat independent dels FPS:** el seguiment fa servir el factor `1 − e^(−k·Δt)`, i es comporta igual a 60 que a 144 FPS.
- **Col·lisió amb una esfera:** fa servir un `SphereCast` (`collisionRadius` = 0,3) en lloc d'un raig, i ignora els Triggers. La càmera ja no s'enganxa a les vores ni s'apropa per culpa del Trigger de la palanca.

#### M4. Salt més permissiu
[SpiderLocomotion.cs:89](Assets/Scripts/Movement/SpiderLocomotion.cs#L89) i [:112](Assets/Scripts/Movement/SpiderLocomotion.cs#L112):
- **Coyote time** (`coyoteTime` = 0,12 s): es pot saltar just després de sortir d'una vora.
- **Buffer de salt** (`jumpBufferTime` = 0,12 s): si es prem Espai just abans de tocar terra, el salt es fa en aterrar.
- **Alçada constant:** abans de l'impuls s'anul·la la velocitat vertical, així el salt és igual de alt baixant una rampa.
- **Detecció del terra amb esfera** (`groundCheckRadius` = 0,4): arriba igual de lluny que el raig original, però no perd el terra a les vores i ignora els Triggers.

#### M5. Salt de paret net
- **Temps d'espera abans de tornar a escalar:** el camp `cooldownTornarAEscalar` (0,25 s) ja existia però no s'usava. Ara, després d'un salt de paret, l'aranya espera aquest temps abans de tornar-se a enganxar ([SpiderClimbing.cs:106](Assets/Scripts/Movement/SpiderClimbing.cs#L106), [:302](Assets/Scripts/Movement/SpiderClimbing.cs#L302)).
- **L'aranya es redreça sola a l'aire:** si queda inclinada, torna suaument a la posició dreta mirant cap enfora de la paret ([SpiderLocomotion.cs:133](Assets/Scripts/Movement/SpiderLocomotion.cs#L133)).

#### M6. Menys feina per fotograma
- **Cronòmetre de la cursa:** ara només refresca el Quest Tracker quan canvia el valor mostrat, 10 vegades per segon en lloc de cada fotograma ([TimeTrialChallenge.cs:53](Assets/Scripts/Quest/QuestImplementation/TimeTrialChallenge.cs#L53)). Així es generen menys textos i el TextMeshPro es reconstrueix menys vegades.
- **`Debug.Log` de la placa de pressió:** s'han comentat. Escriure al registre és lent en un build.

#### M7. Els menús no disparen accions de joc
[InputReader.cs:33](Assets/Scripts/InputReader.cs#L33):
- **Abans:** mentre el joc està aturat (menú, pausa o victòria), clicar un botó també llançava un Swing (clic esquerre) o un Connectar (clic dret), i Espai feia saltar en tornar.
- **Ara:** aquestes accions s'ignoren mentre `Time.timeScale` és 0.

#### M8. El fil ja no dona errors en deixar-lo anar
[SpiderSwinging.cs:166](Assets/Scripts/Movement/SpiderSwinging.cs#L166):
- **Problema:** `Destroy(joint)` no elimina la junta fins al final del fotograma. Mentrestant, `DrawRope` la veia encara viva i intentava dibuixar una línia de 0 punts. Això provocava l'error «SetPosition index out of bounds» cada vegada que es deixava anar el fil.
- **Correcció:** ara la referència es buida a l'instant.

#### M9. Escalada més enganxosa (sobretot als sostres)

**El problema:** a vegades l'aranya es desenganxava dels sostres mentre es movia. Hi havia quatre causes:
- **Un sol pas fallit la deixava anar.** Si els tres raigs que busquen la superfície fallaven durant un sol pas de física, l'aranya es deixava anar. Això passa fàcilment a les cantonades, als sostres corbats i a les juntes entre cares de la malla.
- **No hi havia res que l'enganxés.** Escalant, la gravetat està desactivada, però res no apretava l'aranya contra la superfície. Es movia en línia recta, i qualsevol velocitat que li quedava (per exemple, d'un cop contra el sostre) la feia anar separant fins que els raigs ja no hi arribaven.
- **En enganxar-se, el raig de la panxa apuntava cap a la superfície anterior.** Durant els primers instants només el raig frontal la mantenia enganxada.
- **No hi havia cap raig per a les cantonades exteriors**, com en arribar dalt d'una paret.

**Ara** ([SpiderClimbing.cs:106](Assets/Scripts/Movement/SpiderClimbing.cs#L106) i [:266](Assets/Scripts/Movement/SpiderClimbing.cs#L266)):
- **Marge abans de deixar anar:** només es deixa anar si fa més de `surfaceStickTime` (0,25 s) que no troba la superfície. Mentrestant, s'hi apropa per tornar-la a trobar.
- **Adherència:** cada pas es manté a la distància justa de la superfície (el radi del cos més `surfaceSkin`), corregint a `surfaceSnapSpeed` (5 m/s). També s'anul·la la velocitat que li hagi quedat.
- **En enganxar-se**, fa servir directament la normal de la paret nova.
- **Cantonades exteriors:** un quart raig, des de davant i per sota de la superfície, mira enrere per trobar la cara de l'altra banda.
- **Saltar continua desenganxant a l'instant.** A més, en un sostre ara l'impuls és cap avall, cap enfora del sostre. Abans l'impuls cap amunt empenyia l'aranya contra el sostre ([SpiderClimbing.cs:292](Assets/Scripts/Movement/SpiderClimbing.cs#L292)).
- **Si hi ha un fil enganxat** (Swing o Grappling), mana el fil: no hi ha adherència i l'aranya es deixa anar de seguida, perquè el fil la pugui estirar.

### 5.2 Funcionalitats noves

#### N1. Grappling: recollir el fil (tecla **Q** / gallet **LT**)

Ara existeix la mecànica core que la memòria anomena a 1.3, 4.1, 5.2.1 i 5.3.3 ([SpiderSwinging.cs:113](Assets/Scripts/Movement/SpiderSwinging.cs#L113), [InputReader.cs:109](Assets/Scripts/InputReader.cs#L109)).

- **Com funciona:**
  - Mantenint el botó, si l'aranya no està enganxada es dispara un fil (igual que el Swing).
  - Després, el fil es va escurçant a `grappleReelSpeed` (12 m/s) fins a `minRopeLength` (1,5 m).
- **Què passa segons on s'enganxa el fil:**
  - **Punt fix:** estira l'aranya en línia recta cap a l'ancoratge.
  - **Roca (Rigidbody):** apropa la roca a l'aranya.
- **Combinat amb el Swing:** mantenint el Swing i prement Q durant el balanceig, el fil s'escurça i el pèndol es fa més curt i més ràpid. El fil continua enganxat mentre es mantingui qualsevol dels dos botons.
- **Input:** s'ha afegit l'acció **Grapple** a `GameInput.inputactions`. Abans, l'`InputReader` tenia un mètode `OnGrapple` buit.

#### N2. Comandament d'Xbox

Ara el que diu la memòria a 5.3.2 i 6.2.2.1 és cert.

| Acció | Comandament |
|---|---|
| Moure / Escalar | Joystick esquerre |
| Càmera | Joystick dret (`gamepadSensitivity` = 150 °/s, a `ThirdPersonCamera`) |
| Saltar / Salt de paret | A |
| Córrer | L3 (clic del joystick esquerre) |
| Swing | RT |
| Grappling | LT |
| Connectar | RB |
| Interactuar | X |
| Pausa | Menú (☰) |

- **Càmera:** el ratolí dona un desplaçament per fotograma i el joystick una direcció. Per això l'`InputReader` indica d'on ve l'última entrada (`IsLookFromGamepad`), i la càmera multiplica el valor del joystick pel temps.
- **Menús:** quan s'obre el menú principal, la pausa o la victòria i hi ha un comandament connectat, se selecciona el primer botó per poder navegar sense cursor ([PauseMenu.cs:78](Assets/Scripts/UI/PauseMenu.cs#L78), [MainMenu.cs:75](Assets/Scripts/UI/MainMenu.cs#L75), [MenuVictoria.cs:29](Assets/Scripts/UI/MenuVictoria.cs#L29)).

#### N3. Previsualització dels ponts (Connectar)

[WebConnector.cs:51](Assets/Scripts/Movement/WebConnector.cs#L51), [DynamicCrosshair.cs:32](Assets/Scripts/UI/DynamicCrosshair.cs#L32):
- **Línia de previsualització:** després del primer clic, una línia fina uneix el primer ancoratge amb el punt on apuntes. És **blanca** si el segon punt és vàlid i **vermella** si no.
- **Punt de mira:** es torna **blau cel** quan el segon punt és vàlid.
- **Cancel·lar:** si cliques sense apuntar a cap superfície, el pont a mig fer es cancel·la. Abans, el primer ancoratge es quedava pendent per sempre sense que es veiés.

#### N4. Escalada ràpida

[SpiderClimbing.cs:229](Assets/Scripts/Movement/SpiderClimbing.cs#L229) i [:266](Assets/Scripts/Movement/SpiderClimbing.cs#L266):
- Mantenint Córrer mentre escala, l'aranya va `sprintClimbMultiplier` (1,6) vegades més ràpid.
- Ara és cert el que diu el Repte 1 (5.2.2): «Córrer [...] combinat amb salts i escalada ràpida».

#### N5. Avisos de missió

[MissionManager.cs:42](Assets/Scripts/Quest/MissionManager.cs#L42), [:52](Assets/Scripts/Quest/MissionManager.cs#L52):
- Durant `notificationDuration` (3 s), al capdamunt del Quest Tracker surt destacat en groc «**Nova missió: …**» o «**Missió completada: …!**».
- Fa servir temps sense escalar, així que també desapareix amb el joc aturat.

#### N6. Efecte en recollir objectes

[MissionCollectible.cs:43](Assets/Scripts/Quest/QuestImplementation/MissionCollectible.cs#L43):
- Els cubs i les anelles ja no desapareixen de cop: creixen una mica, giren ràpid i s'encongeixen en `collectAnimationTime` (0,3 s).
- Els col·liders es desactiven a l'instant, perquè l'objecte no es pugui recollir dues vegades.

#### N7. Xarxa de seguretat

[SpiderLocomotion.cs:171](Assets/Scripts/Movement/SpiderLocomotion.cs#L171), [:184](Assets/Scripts/Movement/SpiderLocomotion.cs#L184):
- **On reapareix:** si l'aranya cau fora del mapa, torna a **l'últim punt de terra pla** on havia estat.
- **Quan es considera que ha caigut:** quan és més avall del col·lider més baix del nivell, menys `respawnMargin` (50 m). Aquest límit es calcula sol en començar.
- **Abans:** l'aranya podia caure per sempre i calia reiniciar el nivell.

### 5.3 Text per a la memòria (millores)

**Nou apartat 6.2.2.6, Grappling: recollir el fil (SpiderSwinging.cs):**

> El grappling (tracció lineal cap a un punt d'ancoratge) s'ha implementat com una extensió del sistema de balanceig. Quan el jugador manté el botó de grappling (Q o el gallet esquerre del comandament) i l'aranya encara no està enganxada, es dispara un fil igual que en el Swing. A continuació, a cada pas de física s'escurça la distància màxima del SpringJoint a una velocitat configurable, fins a una longitud mínima. Com que la junta es comporta com un cable gairebé rígid, escurçar-la estira l'aranya en línia recta cap al punt d'ancoratge. Si el fil està lligat a un objecte amb Rigidbody, com la roca del puzle de la placa, el que es desplaça és l'objecte, que s'apropa a l'aranya. Els dos botons es poden combinar: mantenint el Swing i prement el grappling durant el balanceig, el fil s'escurça i el pèndol es fa més curt i més ràpid.

**6.2.2.1, substitueix la frase del comandament per:**

> Això permet que el mateix codi funcioni sense modificacions tant amb teclat i ratolí com amb un comandament d'Xbox: cada acció té assignades les tecles i els botons del comandament a l'arxiu GameInput.inputactions. L'única diferència es tracta a la càmera: el ratolí dona un desplaçament per fotograma, mentre que el joystick dret dona una direcció que s'ha de multiplicar pel temps, i l'InputReader indica d'on ve l'última entrada. Als menús, quan hi ha un comandament connectat, se selecciona automàticament el primer botó per poder navegar sense cursor.

**6.2.2.2, afegeix:**

> **Salt amb marge (coyote time i buffer):** per fer el control més permissiu, el salt no s'executa directament en prémer la tecla. Es guarda el moment en què s'ha premut i, a FixedUpdate, s'executa si l'aranya ha estat a terra en els últims 0,12 segons (coyote time, que permet saltar just després de sortir d'una vora) i la tecla s'ha premut en els últims 0,12 segons (buffer, que permet prémer-la just abans de tocar terra). Abans de l'impuls s'anul·la la velocitat vertical, perquè el salt tingui sempre la mateixa alçada. La detecció del terra fa servir un SphereCast en lloc d'un sol raig, perquè l'aranya no perdi el contacte a les vores.
>
> **Moviment suau:** el Rigidbody fa servir interpolació, ja que la física s'actualitza a 50 Hz i la pantalla a 60 Hz o més; sense interpolació, el personatge es veuria a batzegades.
>
> **Xarxa de seguretat:** si l'aranya cau per sota del col·lider més baix del nivell, torna automàticament a l'últim punt de terra pla on havia estat.

**6.2.1, afegeix:**

> El suavitzat de la posició fa servir un factor exponencial (1 − e^(−k·Δt)), de manera que el seguiment és igual a qualsevol velocitat de fotogrames. La comprovació d'obstacles fa servir un SphereCast (una esfera en lloc d'un raig), que evita que la càmera quedi enganxada a les vores o travessi geometries primes, i ignora els Triggers.

**6.2.2.5, afegeix:**

> **Previsualització:** després del primer clic, una línia fina uneix el primer ancoratge amb el punt on apunta el jugador. És blanca si el segon punt és vàlid i vermella si no ho és. Si el jugador clica sense apuntar a cap superfície, el pont a mig fer es cancel·la.

**6.3.4.2, afegeix:**

> A més, durant tres segons es mostra destacat al capdamunt un avís quan s'activa una missió («Nova missió: ...») o quan es completa («Missió completada: ...!»).

**10. Treball futur:**
- Treu «Afegir suport per a comandament», si l'havies afegit seguint la primera versió d'aquest informe: ja està fet.
- Afegeix, si vols: «Fer que la palanca reaccioni a la tracció del grappling».

### 5.4 Com ajustar o desactivar cada millora

| Millora | On (Inspector) | Camp | Per desactivar-la |
|---|---|---|---|
| Coyote time | Player > SpiderLocomotion | `Coyote Time` | 0 |
| Buffer de salt | Player > SpiderLocomotion | `Jump Buffer Time` | 0 |
| Detecció de terra amb esfera | Player > SpiderLocomotion | `Ground Check Radius` | 0,01 (equival a un raig) |
| Xarxa de seguretat | Player > SpiderLocomotion | `Respawn Margin` | un valor molt gran (p. ex. 100000) |
| Escalada ràpida | Player > SpiderClimbing | `Sprint Climb Multiplier` | 1 |
| Espera després del salt de paret | Player > SpiderClimbing | `Cooldown Tornar A Escalar` | 0 |
| Marge abans de desenganxar-se | Player > SpiderClimbing | `Surface Stick Time` | 0 (més alt = més enganxosa) |
| Adherència a la superfície | Player > SpiderClimbing | `Surface Snap Speed` | 0 |
| Grappling | Player > SpiderSwinging | `Grapple Reel Speed` | 0 |
| Previsualització del pont | Player > WebConnector | `Preview Valid/Invalid Color` | alfa 0 |
| Sensibilitat del comandament | Main Camera > ThirdPersonCamera | `Gamepad Sensitivity` | — |
| Esfera de la càmera | Main Camera > ThirdPersonCamera | `Collision Radius` | 0,01 |
| Avisos de missió | _MissionManagar > MissionManager | `Notification Duration` | 0 |
| Efecte en recollir | cada col·leccionable / anella | `Collect Animation Time` | 0 |
| VSync | Project Settings > Quality > PC | `VSync Count` | Don't Sync |

---

## 6. Descrit a la memòria però no implementat (text proposat)

Les funcionalitats que encara falten van bé a l'apartat **10. Treball futur**, i a les **Conclusions** (on ja dius que no s'han assolit el 100 % dels objectius).

### B1. Grappling — ✅ ara implementat

Vegeu N1 i el text de l'apartat 6.2.2.6 a la secció 5.3. Ja no cal treure'l de la memòria.

### B2. Comandament d'Xbox — ✅ ara implementat

Vegeu N2 i el text per al 6.2.2.1 a la secció 5.3. La frase del 5.3.2 («el botó de menú al comandament d'Xbox») ara és certa.

### B3. Lliurament de les missions a l'NPC

**Apareix a:**
- 6.4: «avisa el MissionManager per donar la missió per lliurada i retirar-ne el seguiment de la pantalla»
- 6.3.3: «completa un objectiu o lliura una missió a un NPC»
- 7: «lliurament de missions»

**Al codi:**
- La missió es completa automàticament quan s'assoleix l'objectiu ([MissionManager.cs:79](Assets/Scripts/Quest/MissionManager.cs#L79)).
- L'NPC només mostra el diàleg d'agraïment.
- Al Quest Tracker, la missió es queda ratllada amb «(Fet!)».

**Proposta:**
- **6.4**, punt «Missió completada»:
  > Si els objectius s'han assolit, mostra el diàleg d'agraïment. La missió ja s'ha donat per completada automàticament en el moment d'assolir l'objectiu, i el Quest Tracker la mostra ratllada amb la marca «(Fet!)».
- **6.3.3:**
  > Cada vegada que una missió arriba a la quantitat requerida, el MissionManager executa un mètode de comprovació (ComprovarVictoriaGlobal)...
- **7:**
  > ...resolució dels reptes (palanques, curses, col·leccionables), finalització de les missions i aparició automàtica de la pantalla de victòria.

### B4. Circuit aeri «flotant a l'aire» i en seqüència

**Apareix a:**
- 5.2.2, Repte 2: «Completar el circuit complet flotant a l'aire»
- 6.5.6: «una seqüència de punts de control»

**Al codi:** les 5 anelles (`AroAeri`) són `MissionCollectible` amb l'id `Cursa_aeria`. Es poden agafar en qualsevol ordre, i tocar terra no reinicia el circuit.

**Proposta:**
- **5.2.2, Resolució:**
  > Passar per les cinc anelles suspeses entre les capçades dels arbres. Com que són a l'aire, la manera d'arribar-hi és encadenar balancejos (i, si cal, recollir el fil amb el grappling): el jugador ha de deixar anar el fil a l'àpex de la paràbola per aprofitar l'impuls i disparar el següent cap a la branca posterior.
- **6.5.6:**
  > Utilitza la mateixa lògica que la recol·lecció d'objectes: cada anella té un Trigger i, quan l'aranya la travessa, suma un punt a la missió i desapareix amb una petita animació. La missió es completa en passar per les cinc anelles, en qualsevol ordre.
- **10. Treball futur:** «Fer que el circuit aeri s'hagi de completar en ordre i sense tocar terra.»

### B5. Moure roques amb «Connectar» i enllaçar objectes físics

**Apareix a:**
- 5.2.4: «Roques: [...] Mitjançant la mecànica de Connectar»
- 5.2.3: «enllaçar dos elements físics independents entre si»

**Al codi:**
- Les roques es mouen amb el **Swing** (i ara també amb el **Grappling**): `SpiderSwinging` crea un `SpringJoint` cap al `Rigidbody` de la roca ([SpiderSwinging.cs:132](Assets/Scripts/Movement/SpiderSwinging.cs#L132)).
- `WebConnector` només crea ponts estàtics.

**Proposta:**
- **5.2.4, Roques:**
  > Objectes que el jugador pot moure indirectament: si hi llança una teranyina amb el Swing, el fil queda lligat a la roca i, quan l'aranya s'allunya, l'arrossega; amb el grappling, recollint el fil, l'apropa.
- **5.2.3, Connectar:**
  > Aquesta mecànica permet fixar una teranyina entre dos punts de l'escenari (amb dos clics successius) i crear-hi un pont transitable. Té una funció dual: d'una banda, permet blocar elements mòbils en una posició (per exemple, un pont que travessa una palanca la manté activada contra la seva força de retorn); de l'altra, permet teixir ponts que serveixen com a noves rutes de pas, tant per al jugador com per resoldre missions amb NPCs atrapats.

### B6. Palanca accionada amb Grappling o amb un botó

**Apareix a:**
- 5.2.4: «disparant un fil de teranyina (Grappling) i exercint una força de tracció»
- 6.5.1: «En prémer el botó [...] es canvia el seu estat intern (isActivated) i s'envia un esdeveniment»

**Al codi ([LeverDoorSystem.cs:30](Assets/Scripts/Quest/QuestImplementation/LeverDoorSystem.cs#L30)):**
- La palanca s'activa per proximitat, o quan un pont de teranyina en travessa el Trigger.
- No hi ha botó, ni variable `isActivated`, ni esdeveniment.
- El grappling existeix, però la palanca no reacciona a la tracció.

**Proposta:**
- **5.2.4, Palanques de paret:**
  > Interruptors amb rotació limitada i força de retorn. S'activen quan l'aranya s'hi col·loca al costat, i es poden deixar fixes travessant-les amb un pont de teranyina (Connectar).
- **6.5.1:** vegeu C5.

### B7. Crosshair amb un estat diferent per a cada mecànica (ara en part)

**Apareix a:** 5.3.3, «indicant [...] si és un punt vàlid per llançar el Grappling, el Swing o per iniciar una connexió».

**Al codi ([DynamicCrosshair.cs:27](Assets/Scripts/UI/DynamicCrosshair.cs#L27)):**
- **Verd:** el punt es pot fer servir per al Swing o el Grappling (capes `Walkable` o `Climbing` a menys de 50 m).
- **Blau cel:** hi ha un pont a mig fer i el segon punt és vàlid.
- **Blanc:** en la resta de casos.

**Proposta (5.3.3 i 6.3.4.1):**
> El Crosshair es torna verd i una mica més gran quan apunta a una superfície on es pot enganxar una teranyina (Swing o Grappling) dins del seu abast, i blau cel quan hi ha un pont a mig fer i el segon punt és vàlid. En la resta de casos es manté blanc semitransparent.

### B8. Opcions del menú de pausa

**El problema:** el 5.3.2 diu «Resume, Controls i Return to menu», però el 6.3.2 i el joc tenien Resume, Restart i Exit.

**Resolt amb els menús nous (secció 2.1):** la pausa ara té Continuar, Controls, Opcions i Menú principal. Això és el que diu el 5.3.2, més Opcions. Al 5.3.2 només cal afegir-hi les Opcions. El que s'ha de canviar és el 6.3.2:

**Proposta (6.3.2):**
> El menú de pausa ofereix quatre opcions: Continuar (reprèn el joc), Controls (mostra la taula de controls), Opcions (obre la pantalla d'opcions) i Menú principal (torna a carregar el nivell, que torna a mostrar el menú inicial).

---

## 7. Text de la memòria desactualitzat (paràgrafs de substitució)

### C1. 6.2.2.3 Sistema de Balanceig amb Teranyines (SpiderSwinging.cs)

La memòria parla d'una «goma elàstica d'acoblament suau», però el codi configura **un cable gairebé rígid** ([SpiderSwinging.cs:150](Assets/Scripts/Movement/SpiderSwinging.cs#L150)):
- `maxDistance` = la distància inicial
- `spring` = 15000
- `damper` = 100

A més, la memòria no explica dues coses: que amb WASD s'aplica força durant el balanceig, i que el fil es pot lligar a objectes amb Rigidbody.

**Substitueix «Implementació Física de la Junta» per:**

> **Implementació Física de la Junta (SpringJoint):** Quan el jugador prem el botó de Swing, es llança un Raycast des del centre de la càmera. Si impacta contra una superfície vàlida (capes Walkable o Climbing) dins de la distància màxima, s'afegeix dinàmicament un component SpringJoint al personatge. La junta es configura com un cable gairebé rígid: la distància màxima és la distància inicial fins al punt d'ancoratge, la mínima és zero i la molla és molt rígida (spring = 15000, damper = 100). Així el fil no s'estira i l'aranya descriu un pèndol que conserva la inèrcia. Mentre penja, les tecles WASD apliquen una força en la direcció de la càmera per guanyar impuls. Si el punt impactat pertany a un objecte amb Rigidbody (per exemple, la roca del puzle de la placa de pressió), la junta s'hi connecta directament i, en allunyar-se, l'aranya arrossega l'objecte. En deixar anar el botó, la junta es destrueix i el personatge surt llançat amb la velocitat acumulada. El mateix fil es pot recollir amb el grappling (apartat 6.2.2.6).

**I «Dibuix en Temps Real» per:**

> **Dibuix en Temps Real de la Teranyina (LineRenderer):** Un component LineRenderer actualitza a cada fotograma la línia entre l'aranya i el punt d'ancoratge. Si el fil està lligat a un objecte mòbil, l'extrem segueix l'objecte.

### C2. 6.2.2.4 Escalada (SpiderClimbing.cs) — el canvi més important

**El problema:**
- El text actual («Navegació en 2.5D», «la rotació del personatge es manté estable en l'eix vertical», «W puja, S baixa») descriu `SpiderClimbing2.cs`, una versió antiga que **no s'usa enlloc**.
- El script real, `SpiderClimbing.cs`, implementa justament l'«algorisme d'orientació de normals» que promet el 5.2.3.
- A més, té un salt de paret que la memòria no esmenta.

**Substitueix tot l'apartat per:**

> Atès que l'aranya ha de poder caminar per qualsevol superfície, s'ha implementat un sistema d'escalada omnidireccional basat en l'orientació de normals.
>
> **Transició de Gravetat:** mentre camina, el personatge llança un raig frontal. Quan detecta una superfície de la capa Climbing (arbres, ponts de teranyina...), desactiva la gravetat del Rigidbody (useGravity = false), desactiva el component de locomoció i passa a l'estat d'escalada.
>
> **Detecció adaptativa:** durant l'escalada es llancen tres raigs: un sota la panxa (en sentit contrari a la normal actual), un endavant i un en diagonal endavant-avall. El diagonal permet girar cantonades convexes, i el frontal permet passar a parets còncaves i a sostres. Si cap no troba res, un quart raig busca la cara de l'altra banda d'una cantonada exterior (per exemple, en arribar dalt d'una paret). La normal resultant s'interpola suaument (Vector3.Slerp) per evitar girs bruscos.
>
> **Adherència:** perquè l'aranya no es desenganxi als sostres ni a les superfícies corbes, a cada pas de física es manté a la distància justa de la superfície i s'anul·la qualsevol velocitat que li hagi quedat, ja que el moviment d'escalada el fa només MovePosition. A més, no es deixa anar per un sol raig fallit: només si fa un quart de segon que no troba la superfície, i mentrestant s'hi apropa per tornar-la a trobar.
>
> **Orientació i moviment:** les direccions de moviment es calculen projectant la càmera sobre el pla de la superfície (amb el producte vectorial entre el vector dret de la càmera i la normal). D'aquesta manera, els controls són relatius al que veu el jugador: davant d'una paret, W puja i S baixa. Mantenint Córrer, l'aranya escala més ràpid. El cos es gira amb Quaternion.LookRotation(direcció, normal), de manera que les potes queden sempre enganxades a la superfície.
>
> **Salt de paret:** si el jugador prem Espai mentre escala, l'aranya surt de l'estat d'escalada a l'instant, recupera la gravetat i rep un impuls cap enfora de la superfície. En una paret, l'impuls també va cap amunt; en un sostre, només cap avall. Durant un quart de segon no es pot tornar a enganxar, i a l'aire es redreça sola.
>
> **Sortida:** si cap raig detecta cap superfície, es restaura la gravetat, el personatge es redreça i es torna a activar la locomoció.

### C3. 6.2.2.5 Connector de Ponts (WebConnector.cs) — la capa

**El problema:**
- La memòria diu: «S'assigna aquest pont dinàmic a la capa de col·lisió Climbing perquè els sensors de terra de l'aranya el considerin un terra sòlid».
- Però el sensor de terra (`SpiderLocomotion`) només mira la capa `Walkable`. La capa `Climbing` la fa servir el sistema d'escalada.

**Substitueix el tercer punt per:**

> S'assigna el pont a la capa Climbing. D'una banda, el sistema d'escalada el reconeix com una superfície on enganxar-se, de manera que l'aranya hi pot caminar encara que estigui inclinat. De l'altra, els reptes (la palanca i la connexió de plataformes) identifiquen els objectes d'aquesta capa com a teranyines.

*(Si fas l'opcional 2.2 i afegeixes `Climbing` a Walkable Layers, també podràs dir que el sensor de terra el detecta i que s'hi pot saltar.)*

### C4. 6.3.4.2 Quest Tracker

**El problema:** la memòria diu que el text s'actualitza «només quan el jugador activa una nova missió o en completa un objectiu». Però durant la cursa també s'actualitza, ara 10 vegades per segon. A més, l'exemple «Animals salvats: 1/4» no coincideix amb el format real, que és `• Salvar animal atrapat: 0 / 1`.

**Proposta:**

> El text s'actualitza quan s'activa una missió o se'n registra progrés. Durant la cursa contrarellotge, el TimeTrialChallenge l'actualitza cada vegada que canvia la dècima de segon mostrada, per ensenyar el temps restant al costat de la missió. Cada missió activa es mostra amb el seu títol i el progrés (per exemple, «• Recull cubs: 2 / 4»), i les completades apareixen ratllades amb la marca «(Fet!)». Si no hi ha cap missió activa, es mostra «Explora l'illa i ajuda els animals».

*(I afegeix-hi el paràgraf dels avisos de la secció 5.3.)*

### C5. 6.5.1 Activar la palanca

**Substitueix la «Implementació Tècnica» per:**

> **Implementació Tècnica (LeverDoorSystem.cs):** la palanca té un Collider configurat com a Trigger i un Rigidbody cinemàtic, necessari perquè detecti els ponts de teranyina, que són col·liders estàtics. Es considera activada mentre l'aranya és dins del Trigger o mentre un pont de teranyina (capa Climbing) el travessa. Mentre està activada, la maneta gira fins a la posició baixada i la porta es desplaça per obrir el pas (interpolació a l'Update). Quan no hi ha res, la maneta torna sola a la posició inicial i la porta es tanca, cosa que simula la força de retorn. La missió «Obra la porta i recull objecte» es completa recollint el col·leccionable associat.

### C6. 6.5.3 Recol·lecció d'objectes col·leccionables

**Correccions:**
- El codi no fa `SetActive(false)`: fa una animació curta i després `Destroy(gameObject)`.
- El progrés només s'accepta si ja s'ha parlat amb l'NPC.

**Proposta:**

> Cada col·leccionable té un Collider configurat com a Trigger. Quan l'aranya hi entra, l'objecte crida MissionManager.Instance.ProgressMission(id, 1). Si la missió està activa (és a dir, el jugador ja ha parlat amb l'NPC), el progrés s'accepta: l'objecte desactiva els seus col·liders, fa una petita animació (creix, gira i s'encongeix) i es destrueix. Si no, l'objecte es queda al seu lloc. Els cubs giren sobre el seu eix per cridar l'atenció.

### C7. 6.5.4 Connexió de plataformes

**El problema:** la memòria diu «Es defineixen dos punts d'ancoratge a l'escena». En realitat, els extrems del pont es poden posar a qualsevol superfície, i l'èxit es detecta amb un volum invisible.

**Proposta:**

> Aquest repte es resol amb el connector de teranyines (WebConnector.cs): el jugador pot ancorar els dos extrems del pont a qualsevol superfície de les capes Walkable o Climbing. Al buit que separa les plataformes hi ha un volum invisible, un Collider configurat com a Trigger amb un Rigidbody cinemàtic i el script PlatformLinkTrigger.cs. Quan el BoxCollider d'un pont (capa Climbing) travessa aquest volum, es considera que les plataformes estan connectades i es suma el progrés a la missió. Si el pont es construeix abans d'haver parlat amb l'NPC, el script ho recorda i completa la missió en el moment en què aquesta s'activa.

### C8. 6.5.5 Cursa

**El problema:** la memòria parla d'«un Trigger d'inici» que «calcula el temps transcorregut». En realitat, la cursa comença quan es parla amb l'NPC, i és un compte enrere.

**Proposta:**

> **Implementació Tècnica:** l'NPC de la cursa té els components MissionNPC i RaceNPC. En parlar-hi, s'activa la missió i es crida TimeTrialChallenge.StartChallenge(), que inicia un compte enrere de 300 segons. El temps restant es mostra al Quest Tracker al costat de la missió (activeTimerText). Si l'aranya entra al Trigger de meta, situat al cim, abans que s'esgoti el temps, la missió es completa. Si el temps arriba a zero, es mostra «Temps esgotat!» i cal tornar a parlar amb l'NPC per tornar-ho a intentar.

### C9. 6.4 NPC — text de l'indicador

**El problema:** a l'escena, l'indicador diu «E per interactuar», no «Prem [E] per parlar».

**Proposta:**

> ...s'activa un indicador visual flotant (visualPrompt) amb el text «E per interactuar», que el script Billboard.cs manté sempre orientat cap a la càmera.

### C10. 12.2 Manual d'usuari (taula de controls)

La taula actual té faltes d'ortografia («Tecali i ratoli», «Corra», «Spai»). Hi falten les tecles alternatives F i G, i ara també el Grappling i el comandament.

**Taula proposada:**

| Acció | Teclat i ratolí | Comandament (Xbox) |
|---|---|---|
| Moure / Escalar | W, A, S, D | Joystick esquerre |
| Mirar / Apuntar | Ratolí | Joystick dret |
| Córrer (i escalar més ràpid) | Shift | L3 |
| Saltar / Salt de paret | Espai | A |
| Swing | Clic esquerre o F | RT |
| Grappling (recollir el fil) | Q | LT |
| Connectar | Clic dret o G | RB |
| Interactuar | E | X |
| Pausa | Esc o P | Menú (☰) |

### C11. 6.3.1 Menú inicial (ara està buit)

**Text nou:**

> El menú inicial és un panell de la mateixa escena del nivell, gestionat pel script MainMenu.cs. En arrencar l'escena, el mètode Start mostra el panell, atura el temps (Time.timeScale = 0), allibera el cursor i amaga el HUD. Ofereix quatre opcions: Jugar amaga el panell, reprèn el temps, torna a mostrar el HUD i bloqueja el cursor al centre per controlar la càmera; Controls mostra una taula amb les tecles i els botons del comandament; Opcions obre la pantalla d'opcions; i Sortir tanca l'aplicació (Application.Quit). El script s'executa després de la càmera ([DefaultExecutionOrder]), perquè aquesta bloqueja el cursor en començar. Si hi ha un comandament connectat, se selecciona el primer botó perquè es pugui navegar sense cursor. Com que el menú forma part de l'escena, tornar al menú principal des de la pausa o des de la pantalla final recarrega el nivell i torna a mostrar el menú.
>
> Totes les pantalles de menú les construeix una eina de l'editor (MenuBuilder.cs, Tools > TFG > Construir menús), de manera que comparteixen colors, tipografia i mides. Les pantalles de Controls i Opcions s'obren per sobre de la pantalla d'on es venia (OverlayPanel.cs) i, en tancar-les amb «Tornar», Esc o el botó B del comandament, s'hi torna.

**Nou apartat, Opcions (OptionsMenu.cs i GameSettings.cs):**

> La pantalla d'opcions s'organitza en tres pestanyes: So (volum general, música i efectes), Càmera (sensibilitat del ratolí i del comandament, i inversió de l'eix vertical) i Pantalla (pantalla completa, qualitat gràfica i sincronització vertical). Amb el comandament, LB i RB canvien de pestanya. Cada control lliscant mostra el seu valor al costat. Els valors es guarden a la classe estàtica GameSettings amb PlayerPrefs, de manera que es mantenen entre partides; quan un valor canvia, GameSettings llança un esdeveniment perquè l'AudioManager actualitzi els volums a l'instant, i la càmera llegeix la sensibilitat a cada fotograma. La qualitat gràfica correspon als nivells de Project Settings > Quality (Baixa i Alta), i en arrencar el joc s'aplica l'última que va triar el jugador.

### C12. 6.3.2 i 6.3.3 (petites addicions)

- **6.3.2**, afegeix:
  > Si el joc ja està aturat per una altra pantalla (menú inicial o victòria), la tecla de pausa s'ignora. Mentre el joc està aturat, l'InputReader tampoc no envia accions de joc noves, de manera que els clics als botons no llancen teranyines.
- **6.3.3**, «Transició d'Estat»:
  > La interfície final es mostra en pantalla, atura el temps de joc (Time.timeScale = 0), allibera el cursor, mostra quant ha durat la partida i ofereix dos botons: tornar al menú principal (ReiniciarJoc, que recarrega l'escena) i sortir de l'aplicació (SortirDelJoc).

---

## 8. Coincideix amb la memòria

- **6.2.1, càmera orbital:**
  - calcula el gir (yaw/pitch) amb el `lookEvent`;
  - fa el seguiment amb `Vector3.Lerp`, amb un òfset configurable (0, 1.5, -10);
  - limita el pitch a ±60°;
  - comprova l'oclusió (ara amb una esfera).
- **6.2, 6.2.2.1, entrades:** l'`InputReader` és un ScriptableObject amb esdeveniments (`moveEvent`, `jumpEvent`, `swingStartedEvent`...) als quals se subscriuen els scripts.
- **6.2.2.2, SpiderLocomotion:**
  - el moviment és relatiu a la càmera, amb la component vertical anul·lada;
  - el gir és suau amb `Quaternion.Slerp`;
  - la detecció de terra mira la capa `Walkable` (distància 1,4);
  - el salt fa servir `ForceMode.Impulse`;
  - córrer multiplica la velocitat per 1,8.
- **6.2.2.3, Swing:** el `Raycast` surt del centre de la càmera, s'afegeix un `SpringJoint` dinàmic i un `LineRenderer` dibuixa el fil.
- **6.2.2.5, WebConnector:**
  - funciona amb dos clics;
  - demana una distància mínima d'1 entre els dos punts;
  - col·loca el pont al punt central;
  - el pont té un `LineRenderer` ample (amplada 1) i un `BoxCollider` orientat, i és a la capa `Climbing`.
- **6.3.2, menú de pausa:** s'obre amb Esc o P a través de l'`InputReader`; té `ResumeGame`, `RestartLevel` i `QuitGame`, i controla `Time.timeScale`.
- **6.3.3, victòria:** `ComprovarVictoriaGlobal` recorre la llista de `MissionData` i mostra la pantalla de victòria amb `Time.timeScale = 0`.
- **6.3.4.2, Quest Tracker:** és un `TextMeshProUGUI` connectat al `MissionManager`.
- **6.4, NPCs:** `MissionNPC` fa servir `OnTriggerEnter`/`OnTriggerExit` amb el tag `Player` i té tres estats de diàleg.
- **6.5.2, placa de pressió:**
  - fa servir `OnTriggerEnter`/`OnTriggerExit` amb un comptador de pes (el jugador o la roca amb el tag `Pushable`);
  - quan hi ha pes, la placa baixa i la porta s'obre; sense pes, la porta es tanca.
  - La missió «Obra la porta amb una pedra» es completa amb el col·leccionable associat.
- **6.5.3 i 6.5.6:** els col·leccionables i les anelles tenen un Trigger i fan servir `ProgressMission`.
- **7, proves:** tots els triggers reaccionen només al tag `Player`.
- **5.2.5 i 5.4:** el joc es completa quan totes les missions estan fetes.
- **Dades coherents:**
  - 6 missions i 6 NPCs;
  - 4 cubs (`CollectItems`) i 2 col·leccionables de recompensa (`Palanca`, `PresurePlate`);
  - 5 anelles (`Cursa_aeria`);
  - 1 roca amb el tag `Pushable` i un Rigidbody.

---

## 9. Codi mort i detalls menors (no s'han tocat)

- **`SpiderClimbing2.cs`:** és la versió antiga de l'escalada (la que descriu el 6.2.2.4). No s'usa en cap escena ni prefab.
- **`PlataformRescueZone.cs`:** no s'usa. La missió de rescat fa servir `PlatformLinkTrigger.cs`.
- **[InputReader.cs:138](Assets/Scripts/InputReader.cs#L138):** `OnPauseMenu` i `OnClimb` són mètodes buits d'accions que ja no existeixen. `OnGrapple` ara sí que funciona (N1).
- **[SpiderSwinging.cs:18](Assets/Scripts/Movement/SpiderSwinging.cs#L18):**
  - `springForce`, `damper` i `massScale` no s'usen: la junta fa servir valors fixos.
  - La classe es diu `SpiderSwingingSimple` dins de l'arxiu `SpiderSwinging.cs`. Unity 6.4 ho accepta, però convé que coincideixin.
- **[LeverDoorSystem.cs:46](Assets/Scripts/Quest/QuestImplementation/LeverDoorSystem.cs#L46):** `ProgressMission(missionID, 0)` no fa res, perquè la missió es completa amb el col·leccionable.
- **[LeverDoorSystem.cs:83](Assets/Scripts/Quest/QuestImplementation/LeverDoorSystem.cs#L83):** `OnTriggerExit` comprova la capa `Walkable` en lloc de `Climbing`. Ara mateix no té efecte perquè els ponts no s'esborren mai, però és inconsistent.
- **[TimeTrialChallenge.cs:69](Assets/Scripts/Quest/QuestImplementation/TimeTrialChallenge.cs#L69):** el text «Fallit!» queda sobreescrit immediatament per «Temps esgotat!» ([línia 58](Assets/Scripts/Quest/QuestImplementation/TimeTrialChallenge.cs#L58)).
- **[MissionNPC.cs:46](Assets/Scripts/Quest/MissionNPC.cs#L46):** no es comprova si `liveDialog` és null.
- **Player:** té un component `PlayerInput` que apunta a un arxiu d'accions que no existeix.
- **Swing:** mentre l'aranya penja del fil, WASD la mou alhora amb `MovePosition` (des de `SpiderLocomotion`) i amb força (des de `SpiderSwinging`). Si notes tremolors en balancejar-te mantenint una tecla, el següent pas seria desactivar el `MovePosition` mentre hi hagi fil i donar més força al Swing. No s'ha canviat perquè cal provar-ho jugant.
- **`Debug.Log`:** en queden a `PlataformRescueZone.cs`, `MenuVictoria.cs` i `MainMenu.cs` (els de la placa ja estan comentats).
- **Noms dels ids de missió:** es comparen com a text exacte (majúscules incloses). L'error A1 venia d'aquí. Si en canvies algun, revisa també l'escena.

---

## 10. Com comprovar-ho

1. **Obre el projecte a Unity 6000.4.3f1.**
   - Deixa que regeneri `GameInput.cs`.
   - La consola no ha de mostrar errors, i `Joc.unity` s'ha de carregar sense avisos de scripts que falten.
2. **Mira la secció 2.** Els menús ja estan muntats; només queda l'apartat opcional.
3. **Prova les correccions en Play Mode:**
   - **Cursa:**
     - Parla amb l'NPC de la cursa, deixa que s'acabi el temps i puja a la meta: la missió **no** s'ha de completar.
     - Torna a parlar-hi i arriba a temps: ara s'ha de completar.
     - Al Quest Tracker només hi ha d'haver una línia de la cursa.
   - **Rescat:**
     - Fes el pont **abans** de parlar amb l'NPC del rescat i després parla-hi: la missió s'ha de completar.
     - Si hi parles sense pont, ha de quedar en curs.
   - **Escalada:** enganxa't a un arbre des de terra i prem Espai. El salt de paret ha de tenir l'alçada normal, i escalar amb WASD ha de funcionar igual que abans.
   - **Roca:** arrossega-la amb el Swing. El fil ha de seguir la roca, i en posar-la sobre la placa la porta s'ha d'obrir.
   - **Càmera:** posa un arbre entre la càmera i l'aranya. La càmera s'ha d'apropar.
   - **Menús:**
     - El joc ha de començar al menú principal, amb el cursor visible i sense el HUD.
     - Controls i Opcions s'han d'obrir i tancar (amb «Tornar» o amb Esc), i Jugar ha de començar el joc.
     - A Opcions, canvia de pestanya i mou els controls: el volum ha de canviar en directe, i en tornar a obrir el joc els valors s'han de mantenir.
     - Esc no ha de fer res al menú principal.
   - **Final:**
     - En lliurar les 6 missions ha de sortir la pantalla de victòria, amb el temps de la partida.
     - Esc no ha de fer res.
     - «Menú principal» ha de tornar al menú principal, i «Sortir» ha de tancar el joc.
4. **Prova les millores:**
   - **Fluïdesa:** camina i balanceja't mirant l'aranya: no ha de fer petits salts visuals.
   - **Salt:** surt caminant d'una vora i prem Espai just després; també prem-lo just abans d'aterrar. En tots dos casos ha de saltar.
   - **Salt de paret:** l'aranya ha de sortir de la paret, redreçar-se a l'aire i no tornar-s'hi a enganxar a l'instant.
   - **Sostres:**
     - Puja per una paret fins a un sostre i camina-hi per sobre en totes direccions, també per sota de les branques corbes. No s'ha de desenganxar.
     - Prem Espai al sostre: s'ha de deixar anar a l'instant i caure cap avall.
     - Arriba dalt de tot d'una paret: ha de passar a la cara de dalt (o, si no és escalable, pujar-hi).
     - Si l'aranya et sembla massa enganxosa o massa poc, ajusta `Surface Stick Time` (secció 5.4).
   - **Grappling:**
     - Apunta a una branca i mantén Q: l'aranya ha de pujar fins a l'ancoratge.
     - Durant un balanceig, mantén el clic esquerre i prem Q: el pèndol s'ha d'escurçar.
     - Amb la roca, mantén Q: s'ha d'apropar.
   - **Connectar:** fes el primer clic dret i mou la càmera. Ha de sortir la línia blanca o vermella i el punt de mira blau cel. Clica dret apuntant al cel: s'ha de cancel·lar.
   - **Escalada ràpida:** escala mantenint Shift; ha d'anar més ràpid.
   - **Avisos:** en parlar amb un NPC ha de sortir «Nova missió: …» en groc durant 3 segons, i en acabar-la, «Missió completada: …!».
   - **Recollir:** els cubs i les anelles han de fer l'efecte abans de desaparèixer.
   - **Comandament (si en tens):**
     - Mou-te, mira, salta i fes servir RT, LT i RB.
     - Pausa amb el botó Menú i navega els menús amb el joystick i A.
   - **Caiguda:** si pots, deixa't caure fora de l'illa. L'aranya ha de tornar a l'últim punt segur.
   - **Clics als menús:** clicar els botons dels menús no ha de llançar teranyines.
5. **Fes un build (.exe)** i repeteix una partida completa.
