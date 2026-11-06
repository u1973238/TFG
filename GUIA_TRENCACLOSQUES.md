# Guia per fer els trencaclosques

Aquesta guia explica quines parts del codi fas servir per muntar un trencaclosques, com es connecten entre elles i com dissenyar-ne de bons. Les peces es creen des de Unity amb **Tools > TFG > Peces de trencaclosques**: cada opció posa la peça al punt on mira la vista de l'escena (Scene view).

**Índex**
1. Com encaixa tot
2. La regla d'or: les capes
3. Les peces, una per una
4. Lligar un trencaclosques a una missió
5. Com dissenyar un trencaclosques
6. Receptes pas a pas (A–G)
7. Proposta: un trencaclosques per zona
8. Quan alguna cosa no funciona
9. Llista de comprovació

---

## 1. Com encaixa tot

Un trencaclosques té tres parts:

1. **El que pot fer l'aranya** (el Player): caminar i saltar, escalar, Swing, Grappling, Connectar (fer ponts) i rebotar sobre un pont (llit elàstic).
2. **Les peces de l'escena**, que reaccionen al que fa l'aranya:
   - **interruptors:** la palanca i la placa de pressió;
   - **mecanismes** que s'obren: la porta, l'ascensor i el pont llevadís;
   - **objectius:** mosques, animals atrapats, cèrcols i objectes per recollir.
3. **La missió**, que dona sentit al trencaclosques. Un NPC la dona, les peces hi sumen punts i, en lliurar-la, l'aranya rep una recompensa. També es poden obrir camins nous.

```
 EL QUE FA L'ARANYA              PECES DE L'ESCENA                              RESULTAT
 Swing / Grappling ── mou ─────► Roca ── pesa sobre ──► Placa de pressió ──┐
 Caminar ──────────── trepitja ───────────────────────► Placa de pressió ──┤
 Connectar (pont) ─── travessa ───────────────────────► Palanca ───────────┼──► PuzzleMover ──► porta / ascensor / pont llevadís
                                                                           │        (pot sumar 1 a una missió)
 Connectar (pont) ─── atrapa ──► Mosca ──────────────────────────────────────────────────────► +1 a la missió
 Connectar (pont) ─── uneix ───► Zona de rescat ──► l'animal creua el pont ──────────────────► +1 a la missió
 Caure sobre un pont ──────────► Llit elàstic ──► salt molt alt
 Swing per l'aire ─────────────► Cèrcols en ordre (RingCircuit) ─────────────────────────────► +1 per cèrcol

 Missió complerta ──► tornar a l'NPC (MissionNPC) ──► recompensa (PlayerUpgrades) ──► MissionUnlock obre camins nous
```

### Els scripts que faràs servir

| Peça | Script | Carpeta | Com es crea |
|---|---|---|---|
| Porta, ascensor, pont llevadís | `PuzzleMover` | `Scripts/Puzzles` | *Peces > Porta amb condicions* o *Ascensor* |
| Placa de pressió | `PressurePlate` | `Scripts/Quest/QuestImplementation` | *Peces > Placa de pressió* |
| Palanca | `LeverDoorSystem` | `Scripts/Quest/QuestImplementation` | *Peces > Palanca* (prefab `Palanca`) |
| Roca | prefab `Roca_Activation` | `Prefabs` | *Peces > Roca mòbil* |
| Ponts de teranyina | `WebConnector` (al Player) i `WebBridge` (a cada pont) | `Scripts/Movement`, `Scripts/Puzzles` | Els fa el jugador |
| Zona amb límit de ponts | `BridgeLimitZone` | `Scripts/Puzzles` | *Peces > Zona amb límit de ponts* |
| Mosca | `Fly` | `Scripts/Puzzles` | *Peces > Mosca amb ruta* |
| Zona de rescat | `PlatformLinkTrigger` | `Scripts/Quest/QuestImplementation` | *Peces > Zona de rescat* |
| Objecte per recollir, cèrcol | `MissionCollectible` | `Scripts/Quest/QuestImplementation` | Prefabs `Collectible` i `AroAeri` |
| Circuit de cèrcols | `RingCircuit` | `Scripts/Puzzles` | *Tools > TFG > Configurar circuit de cèrcols* |
| Fer aparèixer coses | `MissionUnlock` | `Scripts/Quest` | *Add Component* |
| Missió, NPC i gestor | `MissionData`, `MissionNPC`, `MissionManager` | `Scripts/Quest` | *Create > Missions > MissionData*, prefab `NPC` |

Tots els interruptors fan servir la interfície `IPuzzleSwitch`, que només té una propietat, `IsActive`. Per això un `PuzzleMover` pot escoltar qualsevol interruptor. Si mai necessites un interruptor nou (per exemple, un botó que s'activa colpejant-lo amb la roca), n'hi ha prou amb un script que implementi `IPuzzleSwitch`.

---

## 2. La regla d'or: les capes

La capa (*Layer*, a dalt de l'Inspector) de cada objecte decideix què hi pot fer l'aranya. És el que més afecta els trencaclosques:

| Capa | Caminar i saltar | Enfilar-s'hi | Enganxar-hi el Swing o el Grappling | Fer-hi un pont |
|---|:---:|:---:|:---:|:---:|
| **Walkable** | ✅ | ❌ | ✅ | ✅ |
| **Climbing** | (s'hi enfila) | ✅ | ✅ | ✅ |
| **Default** | ⚠️ s'hi aguanta, però no hi pot saltar | ❌ | ❌ | ❌ |

Què vol dir per als trencaclosques:

- **Els terres**, a Walkable.
- **El que es pot escalar** (arbres, roques grans, parets per pujar), a Climbing.
- **Les parets i els sostres que tanquen un trencaclosques**, a Default. Una aranya s'enfila per tot arreu: si la paret del costat d'una porta és escalable, el jugador hi passarà per sobre i el trencaclosques no servirà de res. Passa el mateix amb el Swing: si hi ha una branca a sobre, s'hi pot balancejar i saltar-se l'obstacle.
- **Els punts d'ancoratge que vols que el jugador faci servir** (pals, branques, roques), a Walkable o Climbing, i ben visibles.
- **Les zones invisibles** (col·liders marcats com a *Is Trigger*) no compten: el Swing i el Connectar les travessen.
- **Etiquetes (*Tag*):** les roques porten **Pushable** (és el que detecta la placa) i l'aranya, **Player**.
- **Distàncies:**
  - el Swing i el Grappling arriben fins a 25 m (més amb la recompensa «fil més llarg»);
  - els punts d'un pont han d'estar a menys de 30 m de la càmera;
  - un pont ha de fer més d'1 m.

---

## 3. Les peces, una per una

### Porta, ascensor i pont llevadís (`PuzzleMover`)

Mou un objecte entre la seva posició inicial i una posició «oberta» quan s'activen els interruptors.

| Camp | Per a què serveix |
|---|---|
| *Switches* | Les palanques i plaques que l'obren. Arrossega-hi l'objecte des de la Hierarchy. |
| *Condition* | *All Switches*: s'han d'activar tots alhora. *Any Switch*: n'hi ha prou amb un. |
| *Stay Open Once Opened* | Quan s'obre, ja no es torna a tancar, encara que l'aranya baixi de la placa o desaparegui el pont de la palanca. Fes-lo servir quan el trencaclosques ja està resolt. |
| *Target* | L'objecte que es mou. Buit vol dir el mateix objecte. |
| *Open Offset* | Cap on es mou, en metres. La porta baixa sota terra (Y = −5,2) i l'ascensor puja (Y = 6). |
| *Open Rotation* | Quants graus gira quan està obert (per al pont llevadís, vegeu la recepta G). |
| *Open Duration*, *Close Duration* | Segons que tarda a obrir-se i a tancar-se. En un ascensor, posa'n 3 o 4 perquè l'aranya no surti disparada. |
| *Mission On Open* | Missió que suma 1 la primera vegada que s'obre. |

- **A l'escena:** si el selecciones, una línia taronja mostra on anirà i les línies blaves, a quins interruptors està connectat.
- **Física:** es mou amb un Rigidbody cinemàtic que s'afegeix sol. Així l'aranya i les roques que hi ha a sobre es mouen amb ell.
- **Capa:** la porta es crea a la capa Default, perquè l'aranya no s'hi pugui enfilar.

### Placa de pressió (`PressurePlate`)

S'activa mentre hi ha pes a sobre: una roca (etiqueta Pushable) o l'aranya.

- *Player Counts*: desmarca'l perquè només hi compti una roca («l'aranya no pesa prou»). Sense això, el jugador la pot activar posant-s'hi a sobre, però la porta es torna a tancar quan en baixa. Si la solució és portar-hi una roca, desmarca'l: així queda clar que cal la roca.
- *Door Object*: deixa'l buit i connecta la placa a un `PuzzleMover`, que és més flexible.
- El pes el detecta la caixa invisible (*Box Collider*) de l'objecte `PlacaDePressio`. Si fas la placa més gran, fes més gran també la caixa.

### Roca (`Roca_Activation`)

Té física (Rigidbody), l'etiqueta Pushable i la capa Walkable: s'hi pot enganxar el fil i l'aranya hi pot caminar per sobre.

- **Com es mou:**
  - amb el **Swing**: enganxa el fil a la roca i balanceja't o camina, i la roca t'acompanya;
  - amb el **Grappling** (Q o LT): recull el fil i la roca s'apropa.
- **Compte:** si la roca cau en un lloc d'on no es pot treure, el trencaclosques queda bloquejat. Posa-hi vores, fes que el forat tingui sortida o deixa'n una de recanvi.

### Palanca (`LeverDoorSystem`)

S'activa quan **l'aranya la toca** o quan **un pont de teranyina la travessa**.

- *Stay Activated* (marcat per defecte): un cop activada, la palanca es queda baixada i la porta oberta.
  - Desmarca'l si vols que només aguanti **mentre** l'aranya hi és al costat o un pont la travessa (recepta D).
  - Compte: si el desmarques i la porta és lluny de la palanca, la porta es tornarà a tancar abans que l'aranya hi arribi.
- **Per activar-la de lluny:** al segon clic del pont, apunta a la part de la palanca que es veu. El fil travessa la zona invisible de la palanca i s'enganxa al que hi ha al darrere (el terra o una paret), de manera que el pont passa pel mig de la palanca.
- *Door Object*: deixa'l buit i connecta-la a un `PuzzleMover`.
- El camp *Mission ID* de la palanca no fa res; no cal tocar-lo.

### Ponts de teranyina (`WebConnector` al Player i `WebBridge` a cada pont)

Els fa el jugador: amb el clic dret (o G) marca el primer punt i amb el segon clic, el segon. Mentre apunta, es veu una línia blanca si el pont és vàlid o vermella si no ho és.

- **Capa:** els ponts són a la capa Climbing. L'aranya hi camina i s'hi enfila, i les mosques s'hi enganxen.
- **Llit elàstic:** si l'aranya cau sobre un pont gairebé horitzontal a més de 8 m/s (des d'uns 3,3 m d'alçada), rebota. Puja aproximadament fins a l'alçada des d'on ha caigut, o una mica més. Es configura a *Player > WebConnector* (*Bouncy Bridges*, *Bounce Threshold*, *Bounciness*, *Max Bounce Speed*).
- **Límits per a tot el joc** (*Player > WebConnector*): *Max Bridges* i *Max Bridge Length* (0 vol dir sense límit). Per limitar un sol trencaclosques, fes servir la zona següent.

### Zona amb límit de ponts (`BridgeLimitZone`), peça nova

És una caixa invisible: mentre l'aranya hi és a dins, només pot tenir *Max Bridges* ponts alhora. Si en fa un de més, desapareix el més antic.

- *Max Bridge Length* fa que dins la zona els ponts hagin de ser més curts.
- En entrar-hi, el Quest Tracker avisa del límit; desmarca *Show Notice* si no vols l'avís.
- En sortir-ne, tot torna a ser com abans.
- **Mida:** fes la caixa (*Box Collider > Size*) tan gran com el trencaclosques.
- **Compte:** si en entrar ja tenia més ponts dels permesos, desapareixen els més antics, encara que siguin d'una altra zona.

### Mosca (`Fly`)

Vola seguint els punts de la ruta (`Punt_1`…`Punt_4`). En pots afegir o treure a *Waypoints*, i si la llista és buida, vola donant voltes per la zona (*Wander Radius*).

- Si travessa un pont, s'hi queda enganxada i suma 1 a la missió (*Mission*). Si encara no s'ha acceptat la missió, el punt compta quan s'accepti.
- Si la mosca no té cap model a dins, es crea una mosca de figures bàsiques. Si hi poses un model com a fill, es fa servir el teu.
- **Disseny:** fes que la ruta passi entre dos punts on es pugui enganxar un pont (branques, pals).

### Zona de rescat (`PlatformLinkTrigger`)

És una caixa invisible al buit entre dues plataformes. Quan un pont la travessa, suma 1 a la missió i l'animal atrapat (*Rescued Animal*) **creua el pont**:
1. fa un saltet d'alegria;
2. camina fins a l'extrem del pont que té més a prop;
3. el travessa per sobre dels fils, que tremolen a cada pas;
4. baixa a l'altra banda.

- **On s'atura:** després del pont va fins a *Safe Point* (`PuntSegur`), si n'hi ha; si no, s'atura uns 3 m més enllà.
- **Si troba un NPC pel camí** (per exemple, el que t'ha demanat ajuda), s'hi atura al costat i el mira. La distància es tria a *Stop Near NPC Distance*; 0 vol dir que no s'atura.
- **Com camina:** *Walk Speed*, *Hop Height* i *Step Length*. Si l'animal camina de costat o d'esquena, posa a *Facing Offset* l'angle que cal girar-lo.
- **Mida de la caixa:** fes-la només tan gran com el tros de buit on s'ha de fer el pont. L'animal camina fins al pont, i si el pont és molt lluny haurà de caminar molt.
- *Mission ID* és **un text**: ha de ser exactament l'identificador de la missió (`conectar_plataformas` per a «Salva l'animal atrapat»).
- **Per a diversos animals**, posa una zona per animal i, a la missió, *Required Amount* igual al nombre de zones.

### Objectes per recollir i cèrcols (`MissionCollectible` i `RingCircuit`)

- **`MissionCollectible`** (prefabs `Collectible` i `AroAeri`): quan l'aranya el toca, suma *Value* a la missió *Target Mission ID* i desapareix.
  - *Target Mission ID* és **un text**: `CollectItems` per als cubs i `Cursa_aeria` per als cèrcols.
  - Només es pot recollir mentre la missió està activa.
- **`RingCircuit`** fa que els cèrcols comptin **en ordre** i que el circuit torni a començar si l'aranya toca terra. El cèrcol que toca fa pampallugues.
  - Es configura amb *Tools > TFG > Configurar circuit de cèrcols*, que els ordena pel nom.
  - Encara no s'ha executat a l'escena.

### Fer aparèixer camins (`MissionUnlock`)

Activa o desactiva objectes quan es lliura una missió, o quan s'han lliurat totes (*When All Missions Delivered*). Deixa desactivats a l'escena els objectes que han d'aparèixer.

Per exemple, en lliurar la missió del bosc apareix un pont cap al repte final, i *Notification* mostra «S'ha obert un camí nou!».

---

## 4. Lligar un trencaclosques a una missió

### 4.1 Crea la missió

A `Assets/Scripts/Quest/QuestDefiinitions`, fes clic dret > *Create > Missions > MissionData* i omple:

- *Mission ID*: un identificador únic, sense espais (per exemple, `atrapa_mosques`). **Distingeix majúscules i minúscules.**
- *Title*: el que veurà el jugador al Quest Tracker.
- *Required Amount*: quants punts calen (mosques, animals, cèrcols...).
- *Requires Hand In*: si s'ha de tornar a l'NPC per acabar-la (normalment, sí).
- *Reward* i *Reward Value*: la recompensa (taula de sota).
- *Required Missions*: missions que s'han d'haver lliurat abans d'acceptar aquesta.

### 4.2 Registra-la

Selecciona `_MissionManagar` i afegeix la missió a *All Missions*. **Si no hi és, la missió no avança mai.**

### 4.3 Posa l'NPC

1. Arrossega el prefab `NPC` a l'escena.
2. Al component *Mission NPC*, posa la missió a *Mission To Give*.
3. Escriu els diàlegs: *Dialeg Inicial*, *Dialeg En Progres*, *Dialeg Completat*, *Dialeg Despres* i *Dialeg Bloquejat*. Una pista al diàleg ajuda molt.

### 4.4 Connecta les peces a la missió

| Peça | Camp | Què hi poses |
|---|---|---|
| Porta o ascensor (`PuzzleMover`) | *Mission On Open* | l'asset de la missió |
| Mosca | *Mission* | l'asset |
| Circuit de cèrcols | *Mission* | l'asset |
| Objecte per recollir | *Target Mission ID* | el text de l'ID, exacte |
| Zona de rescat | *Mission ID* | el text de l'ID, exacte |
| Meta de la cursa (`TimeTrialChallenge`) | *Time Trial Mission ID* | el text de l'ID, exacte |

### 4.5 (Opcional) Obre el camí següent

Afegeix un `MissionUnlock` per obrir el camí següent.

### 4.6 Prova-ho

El cicle complet és aquest:
1. Parles amb l'NPC i surt «Nova missió».
2. Resols el trencaclosques i surt «Objectiu complert».
3. Tornes a l'NPC i surten «Missió lliurada» i la recompensa.

### La fletxa de l'objectiu

Durant el joc, una fletxa groga indica la cosa més propera que toca fer:
1. lliurar una missió complerta (l'NPC que la va donar);
2. l'objectiu d'una missió en curs;
3. si no hi ha cap missió en curs, el següent NPC que té una missió per donar.

Si l'objectiu es veu, hi surt un rombe a sobre amb la distància; si no, una fletxa a la vora de la pantalla. Es crea sola: no cal posar-la a l'escena.

Per saber on és l'objectiu d'una missió, la fletxa busca les peces lligades a aquella missió:

| Peça | On apunta |
|---|---|
| Objectes per recollir | L'objecte més proper que queda |
| Circuit de cèrcols | El cèrcol que toca |
| Mosques | La mosca més propera que encara vola |
| Zona de rescat | L'animal atrapat (*Rescued Animal*) |
| Porta o ascensor amb *Mission On Open* | La porta, mentre està tancada |
| Cursa | La meta |

Si una missió no té cap d'aquestes peces, la fletxa apunta a l'NPC que la va donar. Per tant, perquè la fletxa porti el jugador al lloc del trencaclosques, lliga'n alguna peça a la missió.

### Recompenses

| *Reward* | Efecte | *Reward Value* |
|---|---|---|
| `LongerWeb` | El fil del Swing i del Grappling arriba més lluny | 0,25 = +25 % |
| `FasterClimbing` | Escala més ràpid | igual |
| `HigherJump` | Salta més amunt | igual |
| `FasterRunning` | Camina i corre més ràpid | igual |
| `StrongerSwing` | Impulsa més el balanceig | igual |
| `StrongerGrapple` | Recull el fil més ràpid | igual |
| `ExtraBridge` | Un pont més alhora (si hi ha límit de ponts) | nombre de ponts |
| `UnlockGrapple` | Aprèn el Grappling, que fins llavors està bloquejat | — |

Una recompensa també pot ser la clau d'un trencaclosques. Per exemple, una cornisa a la qual només s'arriba amb el salt millorat (`HigherJump`). Prova-ho jugant: un +20 % de força de salt fa saltar aproximadament un 44 % més amunt.

---

## 5. Com dissenyar un trencaclosques

1. **Tria la idea en una frase.** Per exemple: «Per obrir la porta has de portar dues roques a les plaques». Ha de combinar dues mecàniques o més (aquí, el Swing i les plaques).
2. **Dibuixa-la des de dalt**, en un paper: on arriba l'aranya, on és l'objectiu, els obstacles, els punts d'ancoratge i el camí de la solució.
3. **Tanca les dreceres.** Pregunta't com ho faries si no volguessis resoldre'l. Normalment cal:
   - posar les parets i els sostres a Default;
   - treure les branques que permetin passar amb el Swing;
   - vigilar que no es pugui pujar per un arbre del costat.
4. **Munta'l amb peces simples** (*greybox*): cubs i les peces del menú, sense decoració.
5. **Connecta'l:** interruptors al `PuzzleMover`, missió i NPC (secció 4).
6. **Prova'l.** Primer tu, després algú que no l'hagi vist, sense ajudar-lo. Apunta on s'encalla i quant tarda.
7. **Afina'l i decora'l.** Canvia distàncies i afegeix pistes visuals: un fil vell de teranyina que suggereixi on fer el pont, una branca d'un altre color... Al final, posa-hi els models i la decoració.

**Bones pràctiques:**
- **Ensenya abans de demanar:** primer una versió fàcil de la idea (una placa) i, més endavant, la difícil (dues plaques i un forat).
- **Una idea nova per trencaclosques:** cada un hauria de tenir el seu moment de «ja ho tinc!».
- **L'objectiu s'ha de veure** des de l'inici: la porta tancada, la palanca a l'altra banda...
- **No s'ha de poder quedar bloquejat:** compte amb les roques perdudes i els ponts que desapareixen.
- **Durada:** un trencaclosques s'hauria de poder resoldre en 1–5 minuts. Si algú hi està més de 5 minuts, posa una pista al diàleg de l'NPC.

---

## 6. Receptes pas a pas

### A. La porta de les dues roques
**Mecàniques:** Connectar, Swing o Grappling, plaques de pressió.

**Muntatge:**
1. Crea una **Porta amb condicions** i dues **Plaques de pressió**. Desmarca *Player Counts* a les dues plaques.
2. A la porta, posa les dues plaques a *Switches*, deixa *Condition* = *All Switches* i marca *Stay Open Once Opened*.
3. Posa dues **Roques mòbils** a l'altra banda d'un forat.
4. Tanca la sala amb parets a la capa Default.

**Solució:** fer un pont per creuar el forat; enganxar el Swing a cada roca i arrossegar-la (o apropar-la amb el Grappling) fins a la seva placa.

**Compte:** les roques no poden caure on no es puguin recuperar.

### B. Atrapa les mosques
**Mecàniques:** Connectar i observar.

**Muntatge:**
1. Crea una missió nova, «Atrapa les mosques», amb ID `atrapa_mosques` i *Required Amount* = 3.
2. Afegeix-la a *All Missions* i posa un NPC que la doni.
3. Crea tres **Mosques amb ruta** i posa la missió a *Mission*. Mou els punts de cada ruta perquè passin entre dues branques o pals.
4. (Opcional) Posa-hi una **Zona amb límit de ponts** amb *Max Bridges* = 1: el jugador haurà de triar bé on fa cada pont.

**Solució:** mirar per on passa cada mosca i fer un pont que creui la seva ruta.

### C. El salt elàstic
**Mecàniques:** Connectar, escalar, caure i rebotar.

**Muntatge:**
1. Fes una cornisa massa alta per arribar-hi saltant, amb les parets a Default perquè l'aranya no s'hi pugui enfilar.
2. Posa a sota uns **Pals per al llit elàstic** i, a prop, un arbre o una branca (Climbing) **més alt que la cornisa**. El rebot arriba aproximadament a l'alçada des d'on l'aranya s'ha deixat caure.
3. Perquè reboti, l'aranya ha de caure des de més d'uns 3,3 m per sobre del pont.

**Solució:** fer un pont entre els pals, enfilar-se a l'arbre, deixar-se caure sobre el pont i rebotar fins a la cornisa.

**Compte:** el pont ha de ser horitzontal; enganxa els dos extrems a la mateixa alçada.

### D. Pocs fils
**Mecàniques:** Connectar, la palanca i pensar l'ordre.

**Muntatge:**
1. Fes una porta amb una **Palanca** a *Switches*, sense marcar *Stay Open Once Opened*. A la palanca, desmarca *Stay Activated*.
2. Posa la palanca on només s'hi pugui arribar amb un pont, i la porta a l'altra banda d'un buit.
3. Posa una **Zona amb límit de ponts** a sobre de tot, amb *Max Bridges* = 2.

**Solució:** amb un pont a la palanca i un altre per creuar el buit, la porta queda oberta. Si el jugador fa un tercer pont, desapareix el de la palanca i la porta es tanca: ha de pensar on posa cada pont i en quin ordre.

### E. La palanca llunyana
**Mecàniques:** Connectar a distància.

**Muntatge:**
1. Posa una **Palanca** davant d'una paret, a l'altra banda d'un buit que encara no es pugui creuar. Deixa *Door Object* buit.
2. Crea un **Ascensor** o una porta i posa la palanca a *Switches*.
3. Posa un punt d'ancoratge a la banda del jugador: una paret o un pal.

**Solució:** el primer clic, al punt de la teva banda; el segon, apuntant a la palanca. El pont la travessa i l'ascensor puja.

### F. L'ascensor de la roca
**Mecàniques:** Grappling, Swing, la placa i l'ascensor.

**Muntatge:**
1. Crea un **Ascensor** i posa a *Switches* una **Placa de pressió** amb *Player Counts* desmarcat.
2. Deixa la roca a dalt d'una cornisa, i la placa a baix, al costat de l'ascensor.
3. La sortida ha de ser a dalt, on només s'arriba amb l'ascensor.

**Solució:** enganxar el fil a la roca i recollir-lo amb el Grappling perquè caigui de la cornisa; arrossegar-la fins a la placa amb el Swing i pujar a l'ascensor.

### G. El pont llevadís
**Mecàniques:** la palanca (amb l'aranya o amb un pont) i el `PuzzleMover` amb gir.

**Muntatge:**
1. Crea un objecte buit, `Frontissa`, a la vora del buit. Posa-hi a dins un tauló: un Cube a la capa Walkable, posat de peu.
2. A `Frontissa`, afegeix un `PuzzleMover` amb:
   - *Open Offset* a zero;
   - *Open Rotation* amb X = 90, o X = −90 segons cap a on ha de caure;
   - *Switches* = una palanca.

   El tauló gira al voltant del punt on és `Frontissa`.
3. Prova amb Play cap a quina banda cau i canvia el signe si cal.

**Solució:** activar la palanca perquè el pont baixi i creuar el buit.

---

## 7. Proposta: un trencaclosques per zona

Aquesta proposta cobreix els objectius 7 (com a mínim 4 zones) i 8 (com a mínim 3 trencaclosques que combinin dues mecàniques o més), i aprofita les 6 missions que ja tens. Canvia-la com vulguis.

| Zona | Element que la identifica | Trencaclosques | Mecàniques | Missions (recompensa) |
|---|---|---|---|---|
| 1. El bosc (inici) | Un arbre gegant | Recollir els cubs (tutorial) i **E. La palanca llunyana** | Escalar, Connectar | Cubs (escala més ràpid), Palanca (fil més llarg) |
| 2. La pedrera | Roques i plaques | **A. Dues roques** i **F. L'ascensor de la roca** | Swing, Grappling, plaques | Placa (recull el fil més ràpid) |
| 3. L'aiguamoll | Una bassa amb joncs | **B. Les mosques** i el rescat dels animals | Connectar, observar | Rescat (salta més amunt) i una de nova: mosques |
| 4. Les copes dels arbres | Arbres molt alts | **C. Salt elàstic**, la cursa i el circuit de cèrcols | Swing, llit elàstic, escalar | Cursa (corre més ràpid), Circuit (balanceig més fort) |

L'objectiu 7 demana que les zones es puguin visitar en qualsevol ordre. Per això, no les tanquis entre elles: fes servir `MissionUnlock` només per al repte final, quan s'hagin lliurat totes les missions.

---

## 8. Quan alguna cosa no funciona

| Problema | Causa probable | Solució |
|---|---|---|
| La porta no s'obre | L'interruptor no és a *Switches*, o *Condition* = *All Switches* i en falta un | Selecciona la porta: les línies blaves mostren els interruptors connectats |
| La porta es torna a tancar | L'aranya ha baixat de la placa, o la palanca té *Stay Activated* desmarcat i ha desaparegut el pont | Una roca a la placa; *Stay Activated* a la palanca; o *Stay Open Once Opened* al `PuzzleMover` |
| Toco un objecte i no es recull | No s'ha acceptat la missió (surt un avís), o *Target Mission ID* no coincideix amb cap missió (surt un avís a la Console) | Parla amb l'NPC; corregeix l'ID |
| La placa no detecta la roca | La roca no té l'etiqueta Pushable o no entra a la caixa de la placa | Posa-li l'etiqueta i fes més gran el *Box Collider* de la placa |
| L'aranya activa la placa tota sola | *Player Counts* està marcat | Desmarca'l |
| La palanca no s'activa amb el pont | El pont no la travessa | Posa-la davant d'una paret i, al segon clic, apunta-hi |
| No es pot enganxar el fil o fer el pont | La superfície és Default, està massa lluny, o el pont fa menys d'1 m | Capa Walkable o Climbing; apropa els punts |
| La mosca no s'enganxa | La ruta no passa per on es pot fer un pont | Mou els punts de la ruta entre dos ancoratges |
| La missió no avança | No s'ha parlat amb l'NPC, la missió no és a *All Missions*, o l'ID de text no coincideix | Comprova les tres coses (l'ID distingeix majúscules) |
| L'aranya se salta el trencaclosques | Hi ha parets escalables o branques per fer Swing | Posa-les a Default o tapa el pas |
| L'aranya no pot saltar en una plataforma | La plataforma és a Default | Posa-la a Walkable |
| No rebota al llit elàstic | Cau des de massa poc amunt o el pont està inclinat | Més alçada; els dos extrems a la mateixa alçada |
| L'ascensor llança l'aranya | *Open Duration* massa curt | 3–4 segons |

---

## 9. Llista de comprovació

Abans de donar un trencaclosques per acabat:

- [ ] L'has resolt tu començant des de zero.
- [ ] No es pot saltar: has provat de fer trampes escalant, amb el Swing i amb ponts.
- [ ] No es pot quedar bloquejat (roques perdudes, ponts que desapareixen...).
- [ ] Combina dues mecàniques o més.
- [ ] L'objectiu es veu i hi ha alguna pista.
- [ ] Està lligat a una missió que s'accepta, avança, es completa i es lliura.
- [ ] L'ha provat algú que no el coneixia, i l'ha resolt sense ajuda (però no a la primera).
