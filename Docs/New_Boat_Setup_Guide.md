# Návod na přidání a zprovoznění nové lodi v SeaVibe

Tento projekt byl plně zautomatizován a celá herní scéna (fyzika, oceán, vítr, hráč, kormidlování) se generuje na jedno kliknutí. Skript automaticky analyzuje 3D model lodi, vytvoří přesné kolize, umístí na palubu hráče a připraví celou scénu k plavbě.

---

## 1. Vložení nového 3D modelu lodi do projektu

1. **Stažení a umístění modelu:**
   - Stahujte modely z internetu (např. ze Sketchfabu) ideálně ve formátu **.fbx**. Format FBX je pro Unity nejlepší a nejstabilnější volbou, protože obsahuje veškeré textury, materiály a správné měřítko v jednom celistvém balíku.
   - Model (nebo jeho rozbalenou složku ze ZIPu) zkopírujte do složky `Assets` (nebo do její podsložky, např. `Assets/Models/`).
   
2. **Aktualizace zdrojového kódu:**
   - Aby tlačítko "Automaticky vytvořit herní scénu" vědělo o vaší nové lodi, musíte upravit jeden řádek kódu.
   - Otevřete skript `Assets/Core/Editor/SceneSetup.cs`.
   - Najděte zhruba řádek 66 s kódem `AssetDatabase.LoadAssetAtPath<GameObject>(...)`.
   - Změňte cestu uvnitř tak, aby ukazovala přesně na váš nový FBX model.
   Příklad:
   ```csharp
   GameObject boatPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ZefiroBoat/source/ZEFIRO.fbx");
   ```

---

## 2. Automatické vytvoření herní scény

Nemusíte ručně vytvářet fyziku, trup lodi ani dávat lodi těžiště. Skript je napsán tak, že odhalí falešné natočení modelů z internetu (např. modely ležící na boku) a sám si vytvoří vztlakové plováky přesně do rohů nového trupu. 

1. **Příprava scény:**
   - Pokud už ve scéně nějakou starou loď máte, **smažte objekt `Boat` z Hierarchie** (případně založte čistou scénu `File -> New Scene`). Zabráníte tak duplikování a zmatkům.
2. **Spuštění generátoru:**
   - V horním menu Unity klikněte na **`SeaVibe -> Automaticky vytvořit herní scénu`**.
   - Během pár vteřin se ve vaší scéně objeví:
     - **Model Vaší Lodi:** Automaticky zarovnaný a masivně zvětšený na délku **65 metrů** (aby působil jako opravdová jachta). Fyzikální model, ohrady a plováky se přesně přizpůsobí této obří velikosti.
     - **Fyzika a Těžiště:** Loď dostane váhu **15 tun** a její těžiště je dynamicky zavěšeno **hluboko pod úroveň hladiny** (20 % z délky lodě, tedy např. 13 metrů hluboko). To zaručuje, že 65metrovou loď nepřevrátí žádná běžná síla ani chození postavy.
     - **Hráč (FirstPersonController):** Dynamicky naspawnovaný vysoko na obloze přesně nad středem lodi. Díky tomu po startu hry bezpečně dopadne na nejvyšší bod paluby (nebo střechu kajuty) a nehrozí, že by se zasekl uvnitř geometrie.
     - **Voda:** Zcela plochá hladina, připravena k brázdění, nebo i rozbouřený oceán, pokud si ho přepnete.
     - Kormidlo, truhla na sebrání a veškerá neviditelná fyzika trupu.

---

## 3. Testování!

Nyní je vše připraveno.
1. Stiskněte **Play** nahoře v Unity.
2. Pohybujte se myší a klávesami WASD. Postava začne chodit po vaší nové lodi.
3. Loď dokonale reaguje na fyziku vody.
4. Dojděte ke kormidlu, namiřte na něj pohled (musíte být do 3 metrů) a **stiskněte E**.
5. Kamera a hráč se přilepí ke kormidlu. Nyní stiskem **A** nebo **D** otáčíte lodí.
6. Stiskem **E** kormidlo opět pustíte.
