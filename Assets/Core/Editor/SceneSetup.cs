using UnityEngine;
using UnityEditor;
using SeaVibe.Environment;
using SeaVibe.Boat;
using SeaVibe.Player;
using SeaVibe.Interaction;
using SeaVibe.Inventory;

namespace SeaVibe.Editor
{
    public class SceneSetup : EditorWindow
    {
        [MenuItem("SeaVibe/Automaticky vytvořit herní scénu")]
        public static void CreateScene()
        {
            // 0. Úklid starých objektů
            DestroyIfExists("WindManager");
            DestroyIfExists("OceanManager");
            DestroyIfExists("SeaVisual");
            DestroyIfExists("Player");

            // 1. Globální manažeři
            GameObject windManagerObj = new GameObject("WindManager");
            windManagerObj.AddComponent<WindManager>();

            GameObject oceanManagerObj = new GameObject("OceanManager");
            OceanManager om = oceanManagerObj.AddComponent<OceanManager>();
            om.currentState = OceanState.Flat;
            om.ApplyOceanState(OceanState.Flat);

            // 2. Vizuální hladina moře
            GameObject seaVisual = GameObject.CreatePrimitive(PrimitiveType.Plane);
            
            // ZÁSADNÍ OPRAVA: CreatePrimitive automaticky přidává MeshCollider!
            // Oceán nesmí být pevná podlaha, jinak se o něj loď roztříští a spadne na bok.
            DestroyImmediate(seaVisual.GetComponent<Collider>());
            
            seaVisual.name = "SeaVisual";
            seaVisual.transform.localScale = new Vector3(50, 1, 50);
            seaVisual.transform.position = Vector3.zero;
            // Aplikujeme náš vlastní URP shader (WaterSurface.shader), který podporuje Stencil masku
            AssetDatabase.Refresh();
            Shader waterShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Core/Shaders/WaterSurface.shader");
            if (waterShader != null) {
                seaVisual.GetComponent<Renderer>().sharedMaterial = new Material(waterShader);
            } else {
                Debug.LogWarning("Shader WaterSurface nenalezen!");
            }
            
            int waterLayer = LayerMask.NameToLayer("Water");
            if (waterLayer == -1) {
                Debug.LogWarning("Vrstva 'Water' neexistuje. Oceán bude na výchozí vrstvě. Vytvoř v Unity vrstvu 'Water' (ideálně Layer 4).");
                waterLayer = 4;
            }
            seaVisual.layer = waterLayer;
            
            // Přidáme náš aktualizovaný skript pro vlny
            seaVisual.AddComponent<WaterMeshDeformer>();

            // 3. Vytvoření lodi
            GameObject boatObj = GameObject.Find("Boat");
            
            if (boatObj == null)
            {
                GameObject boatPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ZefiroBoat/source/ZEFIRO.fbx");
                if (boatPrefab != null)
                {
                    boatObj = (GameObject)PrefabUtility.InstantiatePrefab(boatPrefab);

                    // --- ZAROVNÁNÍ IMPORTNÍCH CHYB ROTACE ---
                    // Modely z internetu mají často nepatrné náklony (např. -88 stupňů místo -90). 
                    // Kvůli tomu loď plave fyzikálně rovně, ale vizuálně je nakloněná.
                    Vector3 euler = boatObj.transform.eulerAngles;
                    euler.x = Mathf.Round(euler.x / 90f) * 90f;
                    euler.y = Mathf.Round(euler.y / 90f) * 90f;
                    euler.z = Mathf.Round(euler.z / 90f) * 90f;
                    boatObj.transform.eulerAngles = euler;
                    
                    boatObj.name = "Boat";
                    boatObj.transform.position = new Vector3(0, 0, 0);
                    // Velikost a pozice kolizního boxu záleží na tom, jak je model v exportu velký
                    boatObj.AddComponent<BoxCollider>();
                }
                else
                {
                    boatObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    boatObj.name = "Boat";
                    boatObj.transform.position = new Vector3(0, 0, 0);
                    boatObj.transform.localScale = new Vector3(4, 2, 8); 
                }
            }
            
            Rigidbody boatRb = boatObj.GetComponent<Rigidbody>();
            if (boatRb == null) boatRb = boatObj.AddComponent<Rigidbody>();
            boatRb.mass = 2500f;
            boatRb.linearDamping = 0.5f;
            boatRb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            // --- AUTO-ROTACE (Ochrana proti ležícím modelům) ---
            // Modely z internetu (jako J-80) často leží na boku. Pokud uživatel spustil skript
            // na ležící lodi a otočil ji až pak, fyzika se kompletně zbláznila.
            Bounds checkBounds = GetTotalBounds(boatObj);
            
            // Pokud je výška extrémně malá vůči délce a šířce (méně než 20 %), loď zaručeně leží na boku!
            if (checkBounds.size.y < checkBounds.size.x * 0.2f && checkBounds.size.y < checkBounds.size.z * 0.2f) {
                // Loď leží. Rotací o 90 stupňů na ose Z ji u J-80 postavíme svisle!
                boatObj.transform.rotation = Quaternion.Euler(0, 0, 90);
            }
            // ----------------------------------------------------

            // Úklid starých dětí lodi (Masky, plováky), pokud přegenerováváme
            for (int i = boatObj.transform.childCount - 1; i >= 0; i--) {
                Transform child = boatObj.transform.GetChild(i);
                if (child.name.Contains("WaterMask") || child.name.Contains("Floater") || child.name.Contains("ItemPickup") || child.name == "Main Camera") {
                    Undo.DestroyObjectImmediate(child.gameObject);
                }
            }

            // Získáme skutečné rozměry modelu (spočítáním všech podřazených meshů)
            Bounds bounds = GetTotalBounds(boatObj);

            // Nastavíme loď na reálnou velikost 20 metrů (např. běžná jachta)
            // U lodí z internetu nevíme, jestli míří po ose Z nebo X, takže vezmeme tu delší stranu.
            float targetLength = 20f; 
            float currentLength = Mathf.Max(bounds.size.x, bounds.size.z); 
            if (currentLength > 0.01f)
            {
                float scaleMultiplier = targetLength / currentLength;
                boatObj.transform.localScale = boatObj.transform.localScale * scaleMultiplier;
            }
            
            // KRITICKÉ: Musíme přepočítat rozměry (bounds) PO zvětšení modelu, 
            // jinak by se celá fyzika počítala pro miniaturní loď!
            bounds = GetTotalBounds(boatObj);
            
            // KRITICKÁ OPRAVA: Po zvětšení modelu se posunul jeho střed. 
            // Musíme bounds spočítat znovu, jinak by všechno létalo mimo loď!
            bounds = GetTotalBounds(boatObj);
            Vector3 extents = bounds.extents;

            // --- ROBUSTNÍ DETEKCE TVARU LODI ---
            // U modelů z jedné sítě (single-mesh) nelze stěžeň oddělit fyzicky.
            // Použijeme spolehlivý geometrický odhad proporcí lodi.
            float boatLength = Mathf.Max(bounds.size.x, bounds.size.z);
            float boatHeight = bounds.size.y;
            float lowestY = bounds.min.y;
            
            bool isSailboat = (boatHeight / boatLength) > 0.6f;
            
            // Ponor (waterline) od nejnižšího bodu
            // Trup lodi s kýlem tvoří zhruba 30% celkové výšky plachetnice. Ponoříme 80% tohoto trupu.
            float draft = isSailboat ? (boatHeight * 0.3f * 0.8f) : (boatHeight * 0.25f);
            float waterlineY = lowestY + draft;
            
            // Šířka trupu (plachty často přesahují trup do boku, omezíme to)
            float hullWidthX = bounds.size.x;
            float hullWidthZ = bounds.size.z;
            if (isSailboat) {
                float maxHullWidth = boatLength * 0.35f; 
                if (bounds.size.x < bounds.size.z) hullWidthX = Mathf.Min(bounds.size.x, maxHullWidth);
                else hullWidthZ = Mathf.Min(bounds.size.z, maxHullWidth);
            }
            // --- KONEC DETEKCE ---

            // Odstraníme případné předchozí collidery (i ze všech podřazených objektů!)
            foreach(var c in boatObj.GetComponentsInChildren<Collider>()) DestroyImmediate(c);
            
            // Zničíme animátory z modelů, protože ty často natvrdo uzamykají rotaci proti fyzice!
            foreach(var anim in boatObj.GetComponentsInChildren<Animator>()) DestroyImmediate(anim);
            foreach(var anim in boatObj.GetComponentsInChildren<Animation>()) DestroyImmediate(anim);

            // Vytvoříme z BoxColliderů ohrádku
            float t = 0.5f; // tloušťka stěn
            float wallHeight = 3f; // Výška ohrádky
            
            // Podlaha kokpitu bude blízko horního okraje trupu, aby se hráč nepropadl k hladině
            float trueHullHeight = isSailboat ? (boatHeight * 0.3f) : boatHeight;
            float floorY = lowestY + (trueHullHeight * 0.95f); 

            // Podlaha
            BoxCollider cFloor = boatObj.AddComponent<BoxCollider>();
            cFloor.center = boatObj.transform.InverseTransformPoint(new Vector3(bounds.center.x, floorY - t/2f, bounds.center.z));
            Vector3 sFloor = boatObj.transform.InverseTransformVector(new Vector3(hullWidthX, t, hullWidthZ));
            cFloor.size = new Vector3(Mathf.Abs(sFloor.x), Mathf.Abs(sFloor.y), Mathf.Abs(sFloor.z));

            // Levá stěna (-X)
            BoxCollider cLeft = boatObj.AddComponent<BoxCollider>();
            cLeft.center = boatObj.transform.InverseTransformPoint(new Vector3(bounds.center.x - hullWidthX/2f + t/2f, floorY + wallHeight/2f, bounds.center.z));
            Vector3 sLeft = boatObj.transform.InverseTransformVector(new Vector3(t, wallHeight, hullWidthZ));
            cLeft.size = new Vector3(Mathf.Abs(sLeft.x), Mathf.Abs(sLeft.y), Mathf.Abs(sLeft.z));

            // Pravá stěna (+X)
            BoxCollider cRight = boatObj.AddComponent<BoxCollider>();
            cRight.center = boatObj.transform.InverseTransformPoint(new Vector3(bounds.center.x + hullWidthX/2f - t/2f, floorY + wallHeight/2f, bounds.center.z));
            Vector3 sRight = boatObj.transform.InverseTransformVector(new Vector3(t, wallHeight, hullWidthZ));
            cRight.size = new Vector3(Mathf.Abs(sRight.x), Mathf.Abs(sRight.y), Mathf.Abs(sRight.z));

            // Zadní stěna (-Z)
            BoxCollider cBack = boatObj.AddComponent<BoxCollider>();
            cBack.center = boatObj.transform.InverseTransformPoint(new Vector3(bounds.center.x, floorY + wallHeight/2f, bounds.center.z - hullWidthZ/2f + t/2f));
            Vector3 sBack = boatObj.transform.InverseTransformVector(new Vector3(hullWidthX, wallHeight, t));
            cBack.size = new Vector3(Mathf.Abs(sBack.x), Mathf.Abs(sBack.y), Mathf.Abs(sBack.z));

            // Přední stěna (+Z)
            BoxCollider cFront = boatObj.AddComponent<BoxCollider>();
            cFront.center = boatObj.transform.InverseTransformPoint(new Vector3(bounds.center.x, floorY + wallHeight/2f, bounds.center.z + hullWidthZ/2f - t/2f));
            Vector3 sFront = boatObj.transform.InverseTransformVector(new Vector3(hullWidthX, wallHeight, t));
            cFront.size = new Vector3(Mathf.Abs(sFront.x), Mathf.Abs(sFront.y), Mathf.Abs(sFront.z));

            // 3a. Nastavení fyziky lodě (Rigidbody a Vztlak)
            // Změníme těžiště tak, aby odpovídalo skutečnému vizuálnímu středu lodi a bylo posunuto DOLŮ.
            // Kvůli deformovaným FBX modelům musíme najít, která lokální osa je "dolů"
            Rigidbody rb = boatObj.GetComponent<Rigidbody>();
            rb.mass = 2500f; // Váha pro 20m loď
            
            Vector3 localCenter = boatObj.transform.InverseTransformPoint(bounds.center);
            Vector3 localDown = boatObj.transform.InverseTransformDirection(Vector3.down);
            rb.centerOfMass = localCenter + localDown * (boatHeight * 0.4f);
            rb.angularDamping = 3.0f; // Extrémní tlumení rotace, aby se 65m loď přestala houpat
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            // Nastavíme vztlak
            BoatBuoyancy buoyancy = boatObj.GetComponent<BoatBuoyancy>();
            if (buoyancy == null) buoyancy = boatObj.AddComponent<BoatBuoyancy>();
            buoyancy.downwardCoMShift = targetLength * 0.05f; // Těžiště blíž k hladině, 13m bylo moc a fungovalo to jako pomalé kyvadlo
            buoyancy.waterAngularDrag = 3.0f; // Vynutíme extrémní tlumení přímo ve vztlaku, aby nepřepisoval Rigidbody
            buoyancy.waterDrag = 1.5f; // Zvýšíme i odpor vody proti houpání nahoru/dolů
            
            Transform[] newFloaters = new Transform[4];
            
            // Nastavíme 4 rohy bójí (World Space) podle skutečné šířky trupu
            float xOffset = hullWidthX / 2f * 0.8f;
            float zOffset = hullWidthZ / 2f * 0.8f;

            Vector3[] floaterPositions = new Vector3[]
            {
                new Vector3(bounds.center.x - xOffset, waterlineY, bounds.center.z + zOffset),
                new Vector3(bounds.center.x + xOffset, waterlineY, bounds.center.z + zOffset),
                new Vector3(bounds.center.x - xOffset, waterlineY, bounds.center.z - zOffset),
                new Vector3(bounds.center.x + xOffset, waterlineY, bounds.center.z - zOffset)
            };

            for (int i = 0; i < 4; i++)
            {
                GameObject f = new GameObject("Floater_" + i);
                f.transform.SetParent(boatObj.transform);
                f.transform.position = floaterPositions[i]; // Zapisujeme přímo World pozici!
                newFloaters[i] = f.transform;
            }

            // --- DOKONALÁ MASKA LODI (Duplicate Mesh + Internal Sealer) ---
            GameObject maskObj = new GameObject("WaterMask");
            maskObj.transform.SetParent(boatObj.transform, false);
            
            Shader maskShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Core/Shaders/WaterMask.shader");
            Material maskMat = new Material(Shader.Find("Standard"));
            maskMat.color = Color.black; 
            if (maskShader != null) maskMat = new Material(maskShader);
            
            // 1. Zkopírujeme stěny a palubu lodi (přesná silueta)
            foreach (Renderer r in boatObj.GetComponentsInChildren<Renderer>())
            {
                if (r.gameObject.name.Contains("WaterMask")) continue;
                
                Mesh meshToCopy = null;
                if (r is MeshRenderer) {
                    MeshFilter mf = r.GetComponent<MeshFilter>();
                    if (mf != null) meshToCopy = mf.sharedMesh;
                } else if (r is SkinnedMeshRenderer smr) {
                    meshToCopy = smr.sharedMesh;
                }
                
                if (meshToCopy == null) continue;
                
                GameObject meshClone = new GameObject(r.gameObject.name + "_MaskPart");
                meshClone.transform.position = r.transform.position;
                meshClone.transform.rotation = r.transform.rotation;
                meshClone.transform.localScale = r.transform.lossyScale;
                meshClone.transform.SetParent(maskObj.transform, true);
                
                MeshFilter cloneMf = meshClone.AddComponent<MeshFilter>();
                cloneMf.sharedMesh = meshToCopy;
                
                MeshRenderer cloneMr = meshClone.AddComponent<MeshRenderer>();
                
                // KRITICKÉ: Model může mít více submeshů! Musíme dát masku na všechny z nich.
                Material[] maskMats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < maskMats.Length; i++) maskMats[i] = maskMat;
                cloneMr.sharedMaterials = maskMats;
                
                cloneMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // KRITICKÉ: nesmí vrhat stíny
                cloneMr.receiveShadows = false;
            }

            // 2. Přidáme vnitřní ucpávku (Sealer Box), která ucpe díru na schody v palubě
            GameObject sealer = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sealer.name = "Stairwell_Sealer_Mask";
            
            // Naparentujeme přímo na loď, aby se nám dobře počítalo v jejím lokálním prostoru
            sealer.transform.SetParent(boatObj.transform, false);
            sealer.transform.localPosition = boatObj.transform.InverseTransformPoint(new Vector3(bounds.center.x, waterlineY, bounds.center.z));
            sealer.transform.localRotation = Quaternion.identity;
            
            // Zmenšíme ucpávku tak, aby se na 100% vešla do trupu a netrčela ven do vody
            Vector3 localSize = boatObj.transform.InverseTransformVector(new Vector3(hullWidthX * 0.7f, 2f, hullWidthZ * 0.7f));
            sealer.transform.localScale = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
            
            // AŽ POTÉ přeparentujeme na maskObj
            sealer.transform.SetParent(maskObj.transform, true);
            
            DestroyImmediate(sealer.GetComponent<Collider>());
            MeshRenderer sealerMr = sealer.GetComponent<MeshRenderer>();
            sealerMr.sharedMaterial = maskMat;
            sealerMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // KRITICKÉ
            sealerMr.receiveShadows = false;
            // --- KONEC MASKY ---
            
            // Fixní hodnota pro ponor nezávislá na obřím stěžni
            buoyancy.depthBeforeMaxForce = 1.0f; 
            buoyancy.floaters = newFloaters;
            boatObj.AddComponent<BoatController>();
            GameObject itemObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            itemObj.name = "ItemPickup_Wood";
            itemObj.transform.SetParent(boatObj.transform);
            itemObj.transform.localPosition = new Vector3(1.5f, 1.2f, 0f);
            itemObj.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            itemObj.AddComponent<ItemPickup>();
            // Poznámka: itemData si musí uživatel přiřadit ručně z Assets složky

            // 6b. Kormidlo (Zástupný válec)
            GameObject wheelObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wheelObj.name = "SteeringWheel";
            wheelObj.transform.SetParent(boatObj.transform);
            // Umístění kormidla na zádi lodi (relativně)
            wheelObj.transform.position = new Vector3(bounds.center.x, floorY + 1.2f, bounds.center.z - currentLength * 0.3f);
            wheelObj.transform.rotation = Quaternion.Euler(90, 0, 0); // Natočení jako volant
            
            // Kompenzace velikosti rodiče, aby měl válec v reálu přesně 60x10x60 cm
            Vector3 pScale = boatObj.transform.lossyScale;
            wheelObj.transform.localScale = new Vector3(0.6f / pScale.x, 0.1f / pScale.y, 0.6f / pScale.z);
            
            // Kamera pro kormidlování (umístěná nad a za kormidlem)
            GameObject steeringCamObj = new GameObject("SteeringCamera");
            // Nastavíme reálnou pozici nezávisle na deformaci měřítka
            steeringCamObj.transform.position = wheelObj.transform.position + new Vector3(0, 4f, -4f); 
            steeringCamObj.transform.rotation = Quaternion.Euler(20, 0, 0); // Mírně nakloněná dolů
            steeringCamObj.transform.SetParent(wheelObj.transform, true); // true zachová reálnou pozici
            
            Camera steeringCam = steeringCamObj.AddComponent<Camera>();
            steeringCamObj.SetActive(false);
            
            SeaVibe.Interaction.SteeringWheel steeringScript = wheelObj.AddComponent<SeaVibe.Interaction.SteeringWheel>();
            steeringScript.steeringCamera = steeringCam;
            steeringScript.boatController = boatObj.GetComponent<BoatController>();

            // 7. Vytvoření Hráče
            GameObject playerObj = new GameObject("Player");
            // Dynamické umístění postavy: postavit ji přesně doprostřed lodi a shodit ji z výšky (střecha kajuty apod.)
            // Tím zaručíme, že se hráč neobjeví uvnitř žádné geometrie, i když má loď velkou kajutu.
            playerObj.transform.position = new Vector3(bounds.center.x, bounds.max.y + 5.0f, bounds.center.z);
            
            // Nastavení hráče (výška, fyzika)
            FirstPersonController fpController = playerObj.AddComponent<FirstPersonController>();
            Rigidbody playerRb = playerObj.GetComponent<Rigidbody>();
            if (playerRb != null) {
                playerRb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
            }

            // KRITICKÉ: Posuneme celou loď i hráče přesně na hladinu vody (Y = 0 pro vodorysku)
            // Plus musíme přidat přesný ponor rovnováhy (submersion = 1 / floatingPower * depthBeforeMaxForce).
            // floatingPower je 1.5, depthBeforeMaxForce je 1.0 => 1/1.5 * 1.0 = 0.666m pod vodorysku.
            // Tím zabráníme jakémukoliv propadu po startu a houpání.
            float equilibriumDepth = 0.666f;
            boatObj.transform.position = new Vector3(0, -(waterlineY + equilibriumDepth), 0);
            playerObj.transform.position += new Vector3(0, -(waterlineY + equilibriumDepth), 0);

            // Inventář hráče
            playerObj.AddComponent<Inventory.Inventory>();

            // Kamera
            GameObject cameraObj = new GameObject("Main Camera");
            Camera cam = cameraObj.AddComponent<Camera>();
            cameraObj.transform.SetParent(playerObj.transform);
            cameraObj.transform.localPosition = new Vector3(0, 0.6f, 0); 

            Camera mainCam = Camera.main;
            if (mainCam != null && mainCam.gameObject != cameraObj)
            {
                DestroyImmediate(mainCam.gameObject);
            }
            cameraObj.tag = "MainCamera";

            fpController.playerCamera = cameraObj.transform;
            fpController.groundMask = ~0; 

            // Interakce
            PlayerInteraction interaction = playerObj.AddComponent<PlayerInteraction>();
            interaction.playerCamera = cam;
            interaction.interactableLayer = ~0; 
            
            // Propojení hráče s kormidlem
            steeringScript.player = playerObj.transform;
            steeringScript.playerCamera = cam;

            Debug.Log("Úspěch! Scéna SeaVibe byla automaticky vytvořena (Včetně vizuálního oceánu, truhly a inventáře).");
        }

        private static void DestroyIfExists(string name) {
            GameObject obj = GameObject.Find(name);
            if (obj != null) {
                Undo.DestroyObjectImmediate(obj);
            }
        }

        private static Bounds GetTotalBounds(GameObject obj) {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(obj.transform.position, Vector3.zero);
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) {
                b.Encapsulate(renderers[i].bounds);
            }
            return b;
        }
    }
}
