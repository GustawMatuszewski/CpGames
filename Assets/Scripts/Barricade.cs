using UnityEngine;

public class Barricade : MonoBehaviour
{
    public float barricadeHp = 0f;
    public int barricadeLevel = 0;

    [Header("Barricade Hp Values")]
    public float woodHp = 10;
    public float metalHp = 30;

    [Header("Barricade Visual Prefabs")]
    public GameObject plank;
    public GameObject metalSheet;

    [Header("Barricade Visual Settings")]
    public float plankHeight = 0.5f;
    public float metalSheetHeight = 2f;

    public enum Material
    {
        none,
        wood,
        metal
    }

    public Material material = Material.none;

    [Header("Debug MODE!!!")]
    public bool debugMode = false;
    public bool debugLevel0;
    public bool debugLevel1;
    public bool debugLevel2;
    public bool debugLevel3;
    public bool debugLevel4;
    public bool debugLevel5;

    void Update(){
        if (debugMode) DebugControls();
    }

    void DebugControls(){
        int newLevel = barricadeLevel;

        if (debugLevel0) newLevel = 0;
        if (debugLevel1) newLevel = 1;
        if (debugLevel2) newLevel = 2;
        if (debugLevel3) newLevel = 3;
        if (debugLevel4) newLevel = 4;
        if (debugLevel5) newLevel = 5;

        if (newLevel != barricadeLevel){
            barricadeLevel = newLevel;
            RefreshBarricade();
        }
    }

    void calculateBarricadeHp(){
        switch (material){
            case Material.none:
                barricadeHp = 0;
                break;
            case Material.wood:
                barricadeHp = woodHp * barricadeLevel;
                break;
            case Material.metal:
                barricadeHp = metalHp * barricadeLevel;
                break;
        }
    }

    void visualizeBarricade()
    {
        // Delete old visuals
        for (int i = transform.childCount - 1; i >= 0; i--){
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        if (material == Material.none || barricadeLevel <= 0)
            return;

        if (material == Material.wood)
            BuildWoodBarricade();
        else if (material == Material.metal)
            BuildMetalBarricade();
    }

    void BuildWoodBarricade(){
        Collider col = GetComponent<Collider>();
        float bottomY = -col.bounds.size.y / 2 + plankHeight / 2; // local space

        int normalPlanks = Mathf.Min(barricadeLevel, 4);
        for (int i = 0; i < normalPlanks; i++){
            GameObject newPlank = Instantiate(plank, transform);
            newPlank.transform.localPosition = Vector3.up * (bottomY + i * plankHeight);
            newPlank.transform.localRotation = Quaternion.identity; // aligned with barricade
        }

        if (barricadeLevel >= 5){
            float centerY = bottomY + (normalPlanks - 1) * plankHeight / 2f; // middle of previous planks

            GameObject crossPlank = Instantiate(plank, transform);
            crossPlank.transform.localPosition = new Vector3(0, centerY, 0.1f);
            crossPlank.transform.localRotation = Quaternion.Euler(0, 0, Random.value < 0.5f ? -45f : 45f);
        }
    }



    void BuildMetalBarricade(){
        for (int i = 0; i < barricadeLevel; i++){
            GameObject newSheet = Instantiate(metalSheet, transform);
            newSheet.transform.localPosition = new Vector3(0f,0f,0f);
            newSheet.transform.localRotation = Quaternion.identity;
        }
    }

    public void RefreshBarricade(){
        calculateBarricadeHp();
        visualizeBarricade();
    }
}
