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
            om.currentState = OceanState.Rough;
            om.ApplyOceanState(OceanState.Rough);

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
                GameObject boatPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Core/Models/Boat/source/j-80-sailboat/source/model/sketchfab.obj");
                if (boatPrefab != null)
                {
                    boatObj = (GameObject)PrefabUtility.InstantiatePrefab(boatPrefab);
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
            boatRb.mass = 1500f;
            boatRb.linearDamping = 0.5f;
            boatRb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            // Úklid starých dětí lodi (Masky, plováky), pokud přegenerováváme
            for (int i = boatObj.transform.childCount - 1; i >= 0; i--) {
                Transform child = boatObj.transform.GetChild(i);
                if (child.name.Contains("WaterMask") || child.name.Contains("Floater") || child.name.Contains("ItemPickup") || child.name == "Main Camera") {
                    Undo.DestroyObjectImmediate(child.gameObject);
                }
            }

            // Získáme skutečné rozměry modelu (spočítáním všech podřazených meshů)
            Bounds bounds = new Bounds(boatObj.transform.position, Vector3.zero);
            bool hasBounds = false;
            foreach (Renderer r in boatObj.GetComponentsInChildren<Renderer>())
            {
                if (!hasBounds)
                {
                    bounds = r.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(r.bounds);
                }
            }

            // Nastavíme loď tak, aby byla větší (přibližně 20 metrů dlouhá)
            // U lodí z internetu nevíme, jestli míří po ose Z nebo X, takže vezmeme tu delší stranu.
            float targetLength = 20f; 
            float currentLength = Mathf.Max(bounds.size.x, bounds.size.z); 
            float scaleMultiplier = targetLength / currentLength;
            boatObj.transform.localScale = new Vector3(scaleMultiplier, scaleMultiplier, scaleMultiplier);
            
            // KRITICKÁ OPRAVA: Po zvětšení modelu se posunul jeho střed. 
            // Musíme bounds spočítat znovu, jinak by všechno létalo mimo loď!
            bounds = new Bounds(boatObj.transform.position, Vector3.zero);
            foreach (Renderer r in boatObj.GetComponentsInChildren<Renderer>()) {
                bounds.Encapsulate(r.bounds);
            }
            Vector3 extents = bounds.extents;

            // --- VÝPOČET TRUPU (Ignorujeme stěžně a plachty) ---
            float lowestY = float.MaxValue;
            foreach (Renderer r in boatObj.GetComponentsInChildren<Renderer>()) {
                if (r.bounds.min.y < lowestY) lowestY = r.bounds.min.y;
            }
            Bounds hullBounds = new Bounds(boatObj.transform.position, Vector3.zero);
            bool hasHull = false;
            foreach (Renderer r in boatObj.GetComponentsInChildren<Renderer>()) {
                // Přeskočíme vysoké objekty (stěžně a plachty), které tvoří více než 50% celkové výšky lodi
                if (r.bounds.size.y > bounds.size.y * 0.5f) continue;
                
                // Bereme jen objekty, jejichž spodek je blízko dna lodi
                if (r.bounds.min.y <= lowestY + (bounds.size.y * 0.4f)) {
                    if (!hasHull) { hullBounds = r.bounds; hasHull = true; }
                    else { hullBounds.Encapsulate(r.bounds); }
                }
            }
            if (!hasHull) hullBounds = bounds; // Fallback
            // --- KONEC VÝPOČTU TRUPU ---

            // Odstraníme případné předchozí collidery
            foreach(var c in boatObj.GetComponents<Collider>()) DestroyImmediate(c);

            // Vytvoříme z BoxColliderů hrubou "vanu" (podlaha a 4 stěny), aby hráč mohl chodit uvnitř
            // a nevypadl do vody. Zabráníme tím i tomu, aby uvízl v jednom velkém bloku.
            float t = 0.5f; // tloušťka stěn

            // Podlaha
            BoxCollider cFloor = boatObj.AddComponent<BoxCollider>();
            cFloor.center = boatObj.transform.InverseTransformPoint(new Vector3(hullBounds.center.x, hullBounds.min.y + t/2f, hullBounds.center.z));
            cFloor.size = boatObj.transform.InverseTransformVector(new Vector3(hullBounds.size.x, t, hullBounds.size.z));

            // Levá stěna (-X)
            BoxCollider cLeft = boatObj.AddComponent<BoxCollider>();
            cLeft.center = boatObj.transform.InverseTransformPoint(new Vector3(hullBounds.min.x + t/2f, hullBounds.center.y, hullBounds.center.z));
            cLeft.size = boatObj.transform.InverseTransformVector(new Vector3(t, hullBounds.size.y, hullBounds.size.z));

            // Pravá stěna (+X)
            BoxCollider cRight = boatObj.AddComponent<BoxCollider>();
            cRight.center = boatObj.transform.InverseTransformPoint(new Vector3(hullBounds.max.x - t/2f, hullBounds.center.y, hullBounds.center.z));
            cRight.size = boatObj.transform.InverseTransformVector(new Vector3(t, hullBounds.size.y, hullBounds.size.z));

            // Zadní stěna (-Z)
            BoxCollider cBack = boatObj.AddComponent<BoxCollider>();
            cBack.center = boatObj.transform.InverseTransformPoint(new Vector3(hullBounds.center.x, hullBounds.center.y, hullBounds.min.z + t/2f));
            cBack.size = boatObj.transform.InverseTransformVector(new Vector3(hullBounds.size.x, hullBounds.size.y, t));

            // Přední stěna (+Z)
            BoxCollider cFront = boatObj.AddComponent<BoxCollider>();
            cFront.center = boatObj.transform.InverseTransformPoint(new Vector3(hullBounds.center.x, hullBounds.center.y, hullBounds.max.z - t/2f));
            cFront.size = boatObj.transform.InverseTransformVector(new Vector3(hullBounds.size.x, hullBounds.size.y, t));

            // 3a. Nastavení fyziky lodě (Rigidbody a Vztlak)
            Rigidbody rb = boatObj.GetComponent<Rigidbody>();
            if (rb == null) rb = boatObj.AddComponent<Rigidbody>();
            rb.mass = 1000f; // 1 tuna
            rb.linearDamping = 0.5f;
            rb.angularDamping = 1.5f;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            BoatBuoyancy buoyancy = boatObj.GetComponent<BoatBuoyancy>();
            if (buoyancy == null) buoyancy = boatObj.AddComponent<BoatBuoyancy>();
            
            Transform[] newFloaters = new Transform[4];
            
            // Nastavíme 4 rohy bójí (World Space) podle trupu
            float xOffset = hullBounds.extents.x * 0.8f;
            float zOffset = hullBounds.extents.z * 0.8f;
            
            // Posuneme bóje do 75% výšky trupu (místo 40%).
            // Plachetnice mají totiž obří kýl, který tvoří většinu výšky, 
            // takže 40% by znamenalo, že samotná loď bude viset ve vzduchu!
            float hullBottomY = hullBounds.min.y;
            float hullHeight = hullBounds.size.y;
            float waterlineY = hullBottomY + (hullHeight * 0.75f); 

            Vector3[] floaterPositions = new Vector3[]
            {
                new Vector3(hullBounds.center.x - xOffset, waterlineY, hullBounds.center.z + zOffset),
                new Vector3(hullBounds.center.x + xOffset, waterlineY, hullBounds.center.z + zOffset),
                new Vector3(hullBounds.center.x - xOffset, waterlineY, hullBounds.center.z - zOffset),
                new Vector3(hullBounds.center.x + xOffset, waterlineY, hullBounds.center.z - zOffset)
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
            maskObj.transform.SetParent(boatObj.transform);
            maskObj.transform.localPosition = Vector3.zero;
            maskObj.transform.localRotation = Quaternion.identity;
            maskObj.transform.localScale = Vector3.one;
            
            Shader maskShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Core/Shaders/WaterMask.shader");
            Material maskMat = new Material(maskShader);
            if (maskShader == null) Debug.LogWarning("Shader WaterMask nenalezen!");
            
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
            sealer.transform.position = hullBounds.center;
            sealer.transform.rotation = boatObj.transform.rotation;
            
            // Místo složité matematiky s ohraničením (které občas započítá stěžeň a udělá obří krabici)
            // vytvoříme pevnou ucpávku o velikosti 5x5x5 metrů. To spolehlivě ucpe díru na schody,
            // ale nevyleze to ven z lodi (loď má 20 metrů).
            sealer.transform.localScale = new Vector3(5f, 5f, 5f);
            
            // AŽ POTÉ naparentujeme se zachováním world transformace
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

            // 7. Vytvoření Hráče
            GameObject playerObj = new GameObject("Player");
            // Přesné souřadnice pro kormidlo plachetnice
            playerObj.transform.position = new Vector3(-0.14f, 6.26f, -5.64f); 
            
            FirstPersonController fpController = playerObj.AddComponent<FirstPersonController>();
            Rigidbody playerRb = playerObj.GetComponent<Rigidbody>();
            playerRb.constraints = RigidbodyConstraints.FreezeRotation;
            playerRb.collisionDetectionMode = CollisionDetectionMode.Continuous;

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

            Debug.Log("Úspěch! Scéna SeaVibe byla automaticky vytvořena (Včetně vizuálního oceánu, truhly a inventáře).");
        }

        private static void DestroyIfExists(string name) {
            GameObject obj = GameObject.Find(name);
            if (obj != null) {
                Undo.DestroyObjectImmediate(obj);
            }
        }
    }
}
