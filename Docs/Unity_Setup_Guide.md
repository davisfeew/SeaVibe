# Kompletní průvodce nastavením Unity projektu (SeaVibe)

Tento dokument tě krok za krokem provede tím, jak v Unity poskládat vygenerované skripty tak, aby vše od houpání lodě, přes chození až po kormidlování správně fungovalo.

---

## KROK 1: Globální systémy (Oceán a Vítr)

1. **Wind Manager:**
   - Vytvoř ve scéně prázdný GameObject (pravé tlačítko -> `Create Empty`) a pojmenuj ho `WindManager`.
   - Přetáhni na něj skript `WindManager.cs`.
   - Zde si můžeš nastavit směr a sílu větru.

2. **Ocean Manager (Gerstnerovy vlny):**
   - Vytvoř další prázdný GameObject a pojmenuj ho `OceanManager`.
   - Přetáhni na něj skript `OceanManager.cs`.
   - V Inspektoru se automaticky vytvořily 2 vzorové vlny. Tyto vlny matematicky zvedají loď, takže prozatím nepotřebuješ vizuální shader vody, aby fyzika fungovala.

---

## KROK 2: Stavba Lodi (The Boat)

1. **Základní tělo lodi:**
   - Vytvoř 3D objekt, který bude reprezentovat loď (např. obrovská krychle nebo už nějaký model: pravé tlačítko -> `3D Object` -> `Cube`). Pojmenuj ho `Boat`.
   - Objekt musí mít **Collider** (např. `Box Collider`).
   - Přidej na něj komponentu **Rigidbody** (v `Add Component` vyhledej Rigidbody). 
   - V Rigidbody **zvyš Mass** (váhu) alespoň na `1000`.

2. **Houpání na vlnách (Buoyancy):**
   - Přetáhni na loď skript `BoatBuoyancy.cs`.
   - Pod lodí vytvoř 4 prázdné GameObjecty (pravé kliknutí na `Boat` -> `Create Empty`). Pojmenuj je např. `Float1` až `Float4`.
   - Tyto body umísti do **čtyř spodních rohů trupu** (budou fungovat jako "vzduchové polštáře", které tahají loď nahoru).
   - V inspektoru u `BoatBuoyancy` přetáhni tyto 4 body do pole `Floaters`.

3. **Fyzikální řízení (Controller):**
   - Na stejný objekt `Boat` přidej skript `BoatController.cs`. Zde můžeš později měnit sílu otáčení (`Turn Torque`).

---

## KROK 3: Interaktivní Kormidlo (Steering Wheel)

1. **Vytvoření kormidla:**
   - Vytvoř 3D objekt kormidla jako potomka lodi (Pravý klik na `Boat` -> `3D Object` -> `Cylinder`). Pojmenuj ho `SteeringWheel`. Umísti ho tam, kde se má řídit.
   - Ujisti se, že kormidlo má svůj `Collider`.
   - Nastav kormidlu nahoře v inspektoru **Layer** na novou vrstvu (např. klikni na Layer -> Add Layer -> vytvoř "Interactable"). Pak kormidlu tento "Interactable" layer přiřaď.

2. **Nastavení skriptů kormidla:**
   - Přidej na kormidlo skript `SteeringWheel.cs`.
   - Vytvoř u kormidla nový prázdný objekt (Create Empty) a pojmenuj ho `PlayerStandPosition`. Posuň ho přesně tam, kde má postava stát, a otoč ho tak, aby "koukal" dopředu.
   - V Inspektoru `SteeringWheel.cs` přetáhni tento `PlayerStandPosition` do stejnojmenného pole.
   - Do pole `Boat Controller` přetáhni objekt lodi `Boat` (najde si na něm svůj skript).

---

## KROK 4: Nastavení Hráče (Player)

1. **Vytvoření postavy:**
   - Vytvoř nový prázdný GameObject (mimo loď, ať není její potomek před startem hry) a pojmenuj ho `Player`.
   - Přidej na něj skript `FirstPersonController.cs`. (Automaticky to přidá Rigidbody a Capsule Collider).
   - V `Rigidbody` u hráče najdi rozbalovací menu **Constraints** a **zaškrtni Freeze Rotation X, Y i Z**.
   - Posuň `Player` tak, aby stál na lodi.

2. **Kamera a pohled:**
   - Vytvoř kameru (pravý klik na `Player` -> `Camera`).
   - Vynuluj jí pozici (`0, 0, 0`) a pak ji posuň trochu nahoru (Y = cca `0.6`), což reprezentuje oči.
   - V inspektoru hráče u `FirstPersonController` přetáhni tuto kameru do pole `Player Camera`.
   - **Ground Mask:** V `FirstPersonController` najdi pole `Ground Mask` a zaškrtni vrstvu (Layer), ve které je tvá loď (typicky `Default`, případně si pro loď vytvoř vrstvu `Boat` a zaškrtni ji tady). Bez toho hráč nemůže skákat.

3. **Interakční systém:**
   - Na objekt `Player` přidej skript `PlayerInteraction.cs`.
   - Do pole `Player Camera` přetáhni kameru hráče.
   - Do pole `Interactable Layer` vyber z nabídky tu vrstvu, do které jsi dal kormidlo (v mém příkladu "Interactable").

---

## KROK 5: Testování!

Nyní by mělo být vše připraveno.
1. Stiskni **Play** nahoře v Unity.
2. Všimneš si, že loď (krychle) se pod tebou houpe, i když zatím nevidíš texturu vody.
3. Pohybuj se myší a klávesami WASD (nový Input System funguje hned).
4. Dojdi ke kormidlu, namiř na něj pohled (musíš být blízko, dosah je v základu 3 metry) a **stiskni E**.
5. Kamera a hráč se přilepí ke kormidlu. Nyní stiskem **A** nebo **D** začneš otáčet celou lodí pomocí fyziky.
6. Stiskem **E** kormidlo zase pustíš.

> **Tip:** Pokud máš pocit, že se loď při kormidlování netočí, je to proto, že ve skriptu se loď točí lépe, pokud pluje dopředu. Pro otestování zkus v `BoatController.cs` zakomentovat/upravit výpočet `effectiveness`, nebo přidej dopřednou sílu lodi (např. nakloněním plachet - `SailController.cs`).
