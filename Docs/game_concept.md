# Game Design Document: SeaVibe (Pracovní název)

## 1. Úvod a Vize
**Žánr:** First-person námořní simulátor / Survival / Adventura
**Platforma:** PC (případně konzole)
**Cílové publikum:** Hráči hledající atmosférický, odpočinkový, ale zároveň výzvami nabitý zážitek, kteří ocení imerzivní simulaci bez zbytečných čísel a ukazatelů.
**High-Concept:** "Firewatch na otevřeném moři, kde vaší jedinou záchranou a domovem je plachetnice, kterou musíte sami fyzicky ovládat a udržovat."

## 2. Herní Smyčka (Core Game Loop)
Hra je založena na cyklu přípravy, plavby, řešení krizí a odpočinku:
1. **Plánování a Údržba:** Hráč se probudí v podpalubí. Zkontroluje mapu, naplánuje trasu. Vyrobí chybějící prkna z naplaveného dřeva pro případné opravy.
2. **Vyplutí:** Nastavení plachet podle větru, zvednutí kotvy, převzetí kormidla.
3. **Plavba a Přežití:** Během cesty hráč upravuje plachty podle změn větru. Může lovit ryby nebo sbírat suroviny z moře (naplavené barely).
4. **Krize:** Náhlá změna počasí – bouře. Hráč musí povolit plachty, aby se loď nepřevrhla, kormidlovat proti vlnám a opravovat případné průrazy trupu.
5. **Zakotvení a Odpočinek:** Příjezd do klidných vod nebo k ostrovu. Zajištění lodi a odpočinek v podpalubí pro uložení hry.

## 3. Detailní Mechaniky Lodi
Loď není jen prostředek, je to hlavní "postava" hry, o kterou je třeba se starat. Žádné loading obrazovky mezi palubou a interiérem.

### 3.1. Kormidlování
- Kormidlo je interaktivní objekt. Hráč k němu přistoupí a fyzicky s ním točí (pohyby myší/páčky).
- Odezva kormidla závisí na rychlosti lodi a síle vln. Zatočit ve velkých vlnách stojí více námahy a trvá déle.

### 3.2. Plachtění a Vítr
- **Napínání plachet:** Hráč tahá za lana (zvedání/spouštění hlavní plachty).
- **Úhel plachet:** Plachta se musí otáčet vůči větru. Pokud je plachta nastavena kolmo na vítr, loď má maximální tah. Pokud vítr fouká zepředu, hráč musí "křižovat".
- **Přeplachtění:** V silném větru (bouře) nesmí být plně napnuto – hrozí zlomení stěžně nebo převrácení lodě.

### 3.3. Systém Poškození
- **Kolize:** Náraz do útesu nebo obrovské vlny může vytvořit díru do trupu (v podpalubí).
- **Nabírání vody:** Voda fyzicky plní podpalubí. Zvyšuje hmotnost lodi, což zhoršuje ovládání a hrozí potopením.
- **Opravy:** Hráč musí vzít kýbl (vybírání vody přes palubu) a dřevěná prkna (přibití přes díru).

## 4. Vybavení a Podpalubí (Základna)
Vše v lodi má své fyzické místo. Žádné abstraktní inventáře překrývající obrazovku.

- **Pracovní stůl (Workbench):** Slouží k výrobě nástrojů z nasbíraných surovin.
  - *Recepty:* Dřevěné záplaty, pevnější lana (zrychluje manipulaci s plachtami), nový kbelík, rybářský prut, lucerna.
- **Truhla:** Pro uložení materiálů (dřevo, látky, kov) a jídla. Kapacita je omezena vizuálně – uvidíte naskládané věci.
- **Postel:** Slouží pro *Time-skip* a doplnění energie. Nelze spát během bouře (loď se potopí bez dozoru) a během zatékání.
- **Navigační stůl:** Pergamenová mapa. Hráč si pozici nezjistí modrou tečkou, ale musí dedukovat polohu pomocí hvězd, kompasu a tvarů ostrovů.

## 5. UI (Uživatelské rozhraní) a HUD
Hra staví na takzvaném **Diegetickém UI** (hudless design):
- Žádný healthbar. Zdraví poznáte podle dechu postavy a vizuálního omezení obrazovky (zatmívání/tep).
- Žádná minimapa. Používáte fyzický kompas v ruce.
- **Sledování větru:** Sledujete stuhu (nebo praporek) na stěžni pro směr. Pro sílu větru musíte mít buď anemometr, nebo sledovat napnutí plátna a poslouchat zvuk větru.

## 6. Svět a Počasí
- **Cyklus dne a noci:** Hraje obrovskou roli. Noční plavba je nebezpečná kvůli snížené viditelnosti; nutnost zapálit petrolejovou lampu u kormidla.
- **Dynamické počasí:** 
  - *Bezvětří (Calm):* Těžko se pluje, nutnost čekat nebo pádlovat (pokud je přidáno pádlo).
  - *Bříza (Breeze):* Ideální podmínky pro plavbu.
  - *Bouře (Storm):* Extrémní vlny, blesky omezují viditelnost (a mohou uhodit do stěžně). Hráč bojuje o přežití lodi.

## 7. Audiovizuální Styl
- **Grafika:** Stylizace "Firewatch". Výrazné barvy, jednoduché, ale čisté tvary modelů (low-poly), bohaté na postprocesové efekty. Důraz na ohromující západy a východy slunce. Voda je stylizovaná, ale fyzikálně realistická.
- **Zvuk (Audio):** 
  - *ASMR prvek:* Praskání dřeva, narážení vln na trup, uklidňující vítr v lanoví.
  - Zvuky slouží jako herní indikátory (hlučnost praskání dřeva naznačuje, že je loď přetížená nebo hrozí poškození).

## 8. Příběhové a Průzkumné Prvky (Volitelné / Fáze 2)
- Hráč může objevovat malé ostrovy s opuštěnými tábory, majáky nebo vraky lodí, ze kterých může získat unikátní nákresy pro crafting a materiály.
- Příběh může být vyprávěn nepřímo skrz deníky, zprávy v lahvích a pozůstatky jiných námořníků.
