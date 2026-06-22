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
            if (FindAnyObjectByType<WindManager>() != null)
            {
                Debug.LogWarning("Scéna už zřejmě obsahuje objekty SeaVibe. Vytvořte prosím prázdnou scénu (File -> New Scene).");
                return;
            }

            // 1. Globální manažeři
            GameObject windManagerObj = new GameObject("WindManager");
            windManagerObj.AddComponent<WindManager>();

            GameObject oceanManagerObj = new GameObject("OceanManager");
            oceanManagerObj.AddComponent<OceanManager>();

            // 2. Vizuální hladina moře
            GameObject seaVisual = GameObject.CreatePrimitive(PrimitiveType.Plane);
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
            
            Rigidbody boatRb = boatObj.AddComponent<Rigidbody>();
            boatRb.mass = 1500f;
            boatRb.linearDamping = 0.5f;
            boatRb.collisionDetectionMode = CollisionDetectionMode.Continuous;

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
            float targetLength = 20f; 
            float currentLength = bounds.size.z; 
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
                // Bereme jen objekty, jejichž spodek je do 30% celkové výšky lodi (trup)
                if (r.bounds.min.y <= lowestY + (bounds.size.y * 0.3f)) {
                    if (!hasHull) { hullBounds = r.bounds; hasHull = true; }
                    else { hullBounds.Encapsulate(r.bounds); }
                }
            }
            if (!hasHull) hullBounds = bounds; // Fallback
            // --- KONEC VÝPOČTU TRUPU ---

            // BoxCollider přizpůsobíme tak, aby tvořil jen PALUBU lodi (nesmí obalovat stěžeň!)
            // Jinak by se hráč spawnul uvnitř něj a fyzika by explodovala.
            BoxCollider col = boatObj.GetComponent<BoxCollider>();
            if (col == null) col = boatObj.AddComponent<BoxCollider>();
            
            float bottomY = bounds.center.y - extents.y;
            float deckHeight = extents.y * 0.25f; // Trup tvoří zhruba 25% celkové výšky
            float deckCenterY = bottomY + (deckHeight / 2f);
            
            col.center = boatObj.transform.InverseTransformPoint(new Vector3(bounds.center.x, deckCenterY, bounds.center.z));
            col.size = boatObj.transform.InverseTransformVector(new Vector3(extents.x * 2f, deckHeight, extents.z * 2f));

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
            
            // Nastavíme 4 rohy bójí (World Space)
            float xOffset = extents.x * 0.8f;
            float zOffset = extents.z * 0.8f;
            
            // Posuneme bóje do 40% výšky trupu, aby se loď realisticky zanořila do vody
            // a nevznášela se jako korková zátka
            float waterlineY = bottomY + (deckHeight * 0.4f); 

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
            playerObj.transform.position = new Vector3(16.16f, 13.81f, 0f); 
            
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
    }
}
