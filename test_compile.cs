using UnityEngine;
using UnityEditor;

public class Test {
    public void M() {
        GameObject playerObj = null;
        CapsuleCollider playerCol = playerObj.GetComponent<CapsuleCollider>();
        if (playerCol != null) {
            playerCol.height = 1.8f;
            playerCol.center = Vector3.zero;
            playerCol.radius = 0.3f;
        }

        GameObject heroPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MaleCharacter/source/hero/hero.fbx");
        if (heroPrefab != null)
        {
            GameObject heroInstance = UnityEditor.PrefabUtility.InstantiatePrefab(heroPrefab) as GameObject;
            if (heroInstance != null)
            {
                heroInstance.name = "BodyMesh";
                heroInstance.transform.SetParent(playerObj.transform);
                heroInstance.transform.localPosition = new Vector3(0, -0.9f, 0); 
                heroInstance.transform.localRotation = Quaternion.AngleAxis(180f, Vector3.up); 

                Renderer[] renderers = heroInstance.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    Bounds heroBounds = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++) heroBounds.Encapsulate(renderers[i].bounds);
                    float currentHeight = heroBounds.size.y;
                    if (currentHeight > 0.01f)
                    {
                        float scaleMultiplier = 1.8f / currentHeight;
                        heroInstance.transform.localScale = heroInstance.transform.localScale * scaleMultiplier;
                    }

                    Texture2D heroTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/MaleCharacter/source/hero/man_t256.png");
                    if (heroTex != null)
                    {
                        string matPath = "Assets/MaleCharacter/source/hero/HeroMaterial.mat";
                        Material heroMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(matPath);
                        if (heroMat == null)
                        {
                            heroMat = new Material(Shader.Find("Standard"));
                            heroMat.mainTexture = heroTex;
                            UnityEditor.AssetDatabase.CreateAsset(heroMat, matPath);
                        }
                        foreach (Renderer r in renderers) r.sharedMaterial = heroMat;
                    }
                }
            }
        }
    }
}
