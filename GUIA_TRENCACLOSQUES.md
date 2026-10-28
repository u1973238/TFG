# Guia: com muntar els trencaclosques

Aquesta guia explica les peces noves del joc i com combinar-les per fer trencaclosques que demanin pensar (el que reclamava el tutor). Totes les peces es creen des de Unity amb **Tools > TFG > Peces de trencaclosques**. Cada opció posa la peça al punt on mira la vista de l'escena.

---

## 1. Les peces

| Peça | Què fa | Camps importants a l'Inspector |
|---|---|---|
| **Porta amb condicions** (`PuzzleMover`) | Es mou quan s'activen els seus interruptors. | `Switches` (palanques o plaques), `Condition` (*All* = tots / *Any* = qualsevol), `Stay Open Once Opened`, `Open Offset`, `Mission On Open` |
| **Ascensor** (`PuzzleMover`) | Una plataforma que puja quan s'activen els interruptors. | Els mateixos camps que la porta |
| **Placa de pressió** (`PressurePlate`) | S'activa amb pes a sobre. | `Player Counts`: desmarca'l perquè només hi compti una roca |
| **Roca mòbil** | Es pot arrossegar amb el Swing o apropar amb el Grappling. | — |
| **Palanca** (`LeverDoorSystem`) | S'activa quan l'aranya hi és al costat o quan un pont de teranyina la travessa. | `Door Object` es pot deixar buit si la connectes a un `PuzzleMover` |
| **Mosca amb ruta** (`Fly`) | Vola seguint els punts `Punt_1`…`Punt_4`. Si travessa una teranyina, s'hi enganxa. | `Mission` (suma 1 punt per mosca), `Speed`, `Waypoints` |
| **Pals per al llit elàstic** | Dos pals escalables. Un pont entre ells fa de llit elàstic. | — |

**Ponts de teranyina** (`WebConnector`, al Player):
- **Llit elàstic:** si l'aranya hi cau des de més de 3 metres, rebota (`Bouncy Bridges`, `Bounce Threshold`, `Bounciness`).
- **Límits per a trencaclosques** (0 = sense límit):
  - `Max Bridge Length`: llargada màxima d'un pont.
  - `Max Bridges`: quants ponts hi pot haver alhora; si en fas un de més, desapareix el més antic.

---

## 2. Trencaclosques d'exemple

### A. La porta de les dues roques
**Idea:** una porta que només s'obre si hi ha una roca a cada placa. Les roques són a l'altra banda d'un forat.

1. Crea una **Porta amb condicions** i dues **Plaques de pressió**. Desmarca `Player Counts` a les dues, perquè l'aranya no les pugui activar ella sola.
2. A la porta, afegeix les dues plaques a `Switches` i deixa `Condition` = *All Switches*.
3. Posa dues **Roques mòbils** a l'altra banda d'un forat.
4. **Solució:** fer un pont per creuar el forat, enganxar el fil a cada roca i arrossegar-la fins a la seva placa.

### B. Atrapa les mosques
**Idea:** unes mosques volen entre els arbres i cal teixir teranyines on passen.

1. Crea una missió nova (*Create > Missions > MissionData*), per exemple «Atrapa 3 mosques» amb `Required Amount` = 3, i afegeix-la a `All Missions` del `_MissionManagar`.
2. Crea tres **Mosques amb ruta** i assigna'ls aquesta missió. Mou els punts de cada ruta perquè passin entre branques.
3. Afegeix un NPC que doni la missió (`Mission To Give`).
4. **Solució:** observar per on passen i fer ponts que creuin la seva ruta.

### C. El salt elàstic
**Idea:** un replà massa alt per enfilar-s'hi (posa'l en una superfície que no sigui escalable, capa *Walkable* o *Default*).

1. Crea uns **Pals per al llit elàstic** a sota del replà, i una plataforma o branca més alta a prop.
2. **Solució:** fer un pont entre els pals, pujar a l'altra plataforma, deixar-se caure sobre el pont i rebotar fins al replà.

### D. Pont amb pocs fils
**Idea:** al Player, posa `Max Bridges` = 2 per a una zona concreta. El jugador ha de pensar quins dos ponts li calen.

*(Es pot combinar amb A: un pont per creuar i un altre per bloquejar una palanca.)*

### E. La palanca llunyana
**Idea:** una palanca a l'altra banda d'un forat, connectada a un **Ascensor**.

1. Deixa `Door Object` de la palanca buit i afegeix la palanca als `Switches` de l'ascensor.
2. **Solució:** fer un pont des d'una paret de la teva banda fins a la palanca. El pont la travessa, la deixa activada i l'ascensor puja.

---

## 3. Consells de disseny

- **Combinar mecàniques:** cada trencaclosques hauria de demanar dues mecàniques o més (pont + Swing, Grappling + placa, salt elàstic + escalada...).
- **Ensenyar abans de demanar:** primer una versió fàcil i, més endavant, una de més difícil.
- **Marcar els punts d'ancoratge:** posa'ls en superfícies visibles (pals, branques), perquè el jugador entengui on pot enganxar el fil.
- **Provar-ho amb gent:** prova cada trencaclosques amb algú que no l'hagi vist. Si el resol a la primera, és massa fàcil; si no el resol en 5 minuts, cal una pista (per exemple, al diàleg de l'NPC).

---

## 4. Les missions

### Lliurar la missió a l'NPC
- Quan es compleix l'objectiu, el Quest Tracker diu «torna-hi per lliurar-la» i l'NPC mostra un **«?»** groc.
- En parlar-hi, la missió s'acaba del tot i l'aranya rep la **recompensa**.
- La pantalla de victòria surt quan s'han **lliurat** totes les missions.
- Si vols que una missió s'acabi sola, sense tornar a l'NPC, desmarca `Requires Hand In` a l'asset de la missió.

**Indicadors dels NPCs:**

| Indicador | Significat |
|---|---|
| «!» groc | Té una missió per donar |
| «!» gris | Té una missió, però encara està bloquejada |
| «...» | Missió en curs |
| «?» groc | Torna-hi per lliurar-la |

### Recompenses
Cada missió té `Reward` i `Reward Value` (0,25 = un 25 % més). Així estan ara:

| Missió | Recompensa |
|---|---|
| Recull els cubs daurats | Escala més ràpid (+25 %) |
| Obra la porta i recull l'objecte (palanca) | El fil arriba més lluny (+25 %) |
| Obra la porta amb una roca | Recull el fil més ràpid (+40 %) |
| Salva l'animal atrapat | Salta més amunt (+20 %) |
| Cursa contrarellotge | Corre més ràpid (+15 %) |
| Passa per tots els cèrcols | Es balanceja amb més força (+30 %) |

Altres opcions:
- `ExtraBridge`: un pont més a la vegada, si hi ha límit de ponts.
- `UnlockGrapple`: si una missió té aquesta recompensa, el Grappling comença bloquejat i s'aprèn en lliurar-la.

### Cadena de missions
- A `Required Missions` hi poses les missions que s'han de lliurar abans. Fins llavors, l'NPC diu que encara no estàs preparada i quina missió falta.
- Ara mateix:
  - la **cursa** es desbloqueja després dels **cubs**: escalar més ràpid ajuda a la cursa;
  - els **cèrcols** es desbloquegen després de la **palanca**: un fil més llarg ajuda a balancejar-se.
- **Fer aparèixer coses:** afegeix un objecte amb el component `MissionUnlock` per mostrar coses quan es lliura una missió, o quan s'han lliurat totes. Per exemple, un pont cap a una zona nova, un NPC nou o el repte final. Deixa aquests objectes desactivats a l'escena.

### Circuit de cèrcols en ordre
1. Executa **Tools > TFG > Configurar circuit de cèrcols**. Troba els 5 cèrcols, els posa en ordre pel nom i els connecta a la missió.
2. Revisa l'ordre a la llista `Rings` del `RingCircuit` i desa l'escena.
3. Ara només compta el cèrcol que toca, que fa pampallugues. Si l'aranya toca terra a mig circuit, tot torna a començar (`Reset On Ground`).

### Diversos animals atrapats
1. Crea una **Zona de rescat** per a cada animal (*Tools > TFG > Peces de trencaclosques*) i posa-la al buit entre plataformes.
2. Arrossega l'animal a `Rescued Animal` i mou `PuntSegur` a l'altra banda: quan el pont estigui fet, l'animal hi anirà saltant.
3. A la missió «Salva l'animal atrapat», posa `Required Amount` igual al nombre de zones, i canvia el títol a «Salva els animals atrapats».
