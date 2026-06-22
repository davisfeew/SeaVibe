# Návod na přidání a zprovoznění nové lodi v SeaVibe

Tento návod popisuje, jak krok za krokem přidat do projektu nový 3D model lodi, nastavit mu správně textury a zajistit, aby jej automatický generátor scény dokázal bez problémů zpracovat (včetně ořezu vody a fyziky).

## 1. Příprava modelu a textur
Když si stáhnete novou loď z internetu (často ve formátu `.zip`), proveďte následující:
1. Rozbalte soubor a zkopírujte celou jeho složku do Unity projektu do umístění `Assets/Core/Models/Boat/source/NazevVasiLode`.
2. Pokud rozbalená složka obsahuje další zazipovaný archiv (například `sketchfab.zip`), rozbalte ho také. Unity neumí číst zabalené `.zip` soubory přímo.
3. Najděte samotný 3D model (nejčastěji má koncovku `.obj` nebo `.fbx`).
4. Ujistěte se, že obrázky (textury) leží hned vedle modelu nebo ve složce `textures` v jeho blízkosti, aby je Unity dokázalo najít.

## 2. Nastavení materiálů (Oprava chybějících textur)
U formátu `.obj` z internetu Unity často nedokáže automaticky spárovat materiály s texturami a loď pak vypadá jednobarevně (např. šedě nebo béžově). Toto musíte vždy udělat ručně pro každý nový model:
1. V Unity otevřete složku s modelem lodi a rozbalte jej (kliknutím na šipku vedle něj), abyste viděli jeho jednotlivé části (např. *Hull*, *Deck*, *Sail*).
2. U každé části najděte v panelu **Inspector** rozbalovací záložku pro **Material**.
3. Unity obvykle vytvoří v projektu složku `Materials` hned vedle modelu. Jděte do ní a vyberte materiál pro danou část.
4. Najděte své obrázky (textury) pro daný materiál.
5. Obrázek s barvou (nejčastěji končící na `_D`, `_Base` nebo `_Albedo`) přetáhněte v Inspectoru do čtverečku vedle nápisu **Base Map** (nebo **Albedo**).
6. Obrázek s normálami (fialovo-modrý, končící na `_N` nebo `_Normal`) přetáhněte do čtverečku vedle **Normal Map**.
7. Zkontrolujte, zda loď nyní vypadá kompletně a správně.

> [!TIP]
> Pokud model po importu obsahuje díry (například chybějící části paluby nebo zábradlí), jedná se o vadu samotného 3D modelu. Pro perfektní výsledek doporučujeme používat ucelené modely bez děr do podpalubí.

## 3. Úprava generátoru (SceneSetup.cs)
Aby automatické tlačítko `SeaVibe -> Automaticky vytvořit herní scénu` načítalo vaši novou loď, je třeba upravit jeden řádek kódu.
1. Otevřete skript `Assets/Core/Editor/SceneSetup.cs`.
2. Najděte sekci `// 3. Vytvoření lodi`.
3. Změňte cestu u metody `LoadAssetAtPath` tak, aby ukazovala přesně na váš nový 3D model lodi. 
Příklad:
```csharp
GameObject boatPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Core/Models/Boat/source/MojeNovaLod/model.obj");
```
4. Soubor uložte.

## 4. Spuštění a vygenerování scény
1. Než znovu spustíte generátor, je **Kriticky důležité** smazat celou starou loď z Hierarchie ve vaší aktuální scéně (smažte objekt `Boat` a raději pro jistotu i `WaterMask`).
   - Můžete také použít prázdnou scénu: `File -> New Scene`.
2. Následně klikněte na horní menu **SeaVibe -> Automaticky vytvořit herní scénu**.
3. Skript novou loď najde, zvětší na 20 metrů, automaticky kolem ní vygeneruje neviditelnou masku ořezávající vodu, nasadí bóje pro fyziku plavání a zapojí kameru a hráče.

Hotovo! Voda by nyní měla dokonale obtékat trup vaší nové lodi a nezaplombovat ji zevnitř.
