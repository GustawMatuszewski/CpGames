using UnityEngine;
using System.Collections.Generic;

public class Barricade : MonoBehaviour, IInteractable {
    public bool useSnapping = true;
    public List<Transform> snapPoints = new List<Transform>();
    public Transform lookAtPoint;
    public bool UseSnapping => useSnapping;
    public List<Transform> InteractionPositions => snapPoints;
    public Transform LookAtTarget => lookAtPoint;

    public float barricadeHp = 0f;
    public int barricadeLevel = 0;

    public float woodHp = 10;
    public float metalHp = 30;

    public GameObject plank;
    public GameObject metalSheet;

    public float plankHeight = 0.5f;
    public float metalSheetHeight = 2f;

    public float buildCooldownPerLevel = 2f;

    float nextBuildTime;

    public enum Material {
        none,
        wood,
        metal
    }

    public Material material = Material.none;

    public bool debugMode = false;
    public bool debugLevel0, debugLevel1, debugLevel2, debugLevel3, debugLevel4, debugLevel5;

    List<GameObject> spawnedParts = new List<GameObject>();

    void Update() {
        if (debugMode) DebugControls();
    }

    public void OnInteract() {
        if (Time.time < nextBuildTime) return;
        if (barricadeLevel >= 5) return;

        barricadeLevel++;
        nextBuildTime = Time.time + buildCooldownPerLevel;
        RefreshBarricade();
    }

    void DebugControls() {
        int newLevel = barricadeLevel;
        if (debugLevel0) newLevel = 0;
        if (debugLevel1) newLevel = 1;
        if (debugLevel2) newLevel = 2;
        if (debugLevel3) newLevel = 3;
        if (debugLevel4) newLevel = 4;
        if (debugLevel5) newLevel = 5;

        if (newLevel != barricadeLevel) {
            barricadeLevel = newLevel;
            RefreshBarricade();
        }
    }

    void calculateBarricadeHp() {
        if (material == Material.none) barricadeHp = 0;
        if (material == Material.wood) barricadeHp = woodHp * barricadeLevel;
        if (material == Material.metal) barricadeHp = metalHp * barricadeLevel;
    }

    void visualizeBarricade() {
        for (int i = spawnedParts.Count - 1; i >= 0; i--) {
            if (spawnedParts[i] != null) {
                if (Application.isPlaying) Destroy(spawnedParts[i]);
                else DestroyImmediate(spawnedParts[i]);
            }
        }
        spawnedParts.Clear();

        if (material == Material.none || barricadeLevel <= 0) return;
        if (material == Material.wood) BuildWoodBarricade();
        if (material == Material.metal) BuildMetalBarricade();
    }

    void BuildWoodBarricade() {
        Collider col = GetComponent<Collider>();
        if (col == null) return;

        float bottomY = -col.bounds.size.y / 2 + plankHeight / 2;
        int normalPlanks = Mathf.Min(barricadeLevel, 4);

        for (int i = 0; i < normalPlanks; i++) {
            GameObject p = Instantiate(plank, transform);
            p.transform.localPosition = Vector3.up * (bottomY + i * plankHeight);
            p.transform.localRotation = Quaternion.identity;
            spawnedParts.Add(p);
        }

        if (barricadeLevel >= 5) {
            float centerY = bottomY + (normalPlanks - 1) * plankHeight / 2f;
            GameObject c = Instantiate(plank, transform);
            c.transform.localPosition = new Vector3(0, centerY, 0.1f);
            c.transform.localRotation = Quaternion.Euler(0, 0, Random.value < 0.5f ? -45f : 45f);
            spawnedParts.Add(c);
        }
    }

    void BuildMetalBarricade() {
        for (int i = 0; i < barricadeLevel; i++) {
            GameObject m = Instantiate(metalSheet, transform);
            m.transform.localPosition = Vector3.zero;
            m.transform.localRotation = Quaternion.identity;
            spawnedParts.Add(m);
        }
    }

    public void RefreshBarricade() {
        calculateBarricadeHp();
        visualizeBarricade();
    }
}