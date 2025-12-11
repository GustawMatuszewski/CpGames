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
    public float plankHeight = 0.25f; // Added for flexible wood spacing
    public float metalSheetHeight = 0.35f;

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

    private Transform barricadePosition;

    void Start()
    {
        // Check if a visual root already exists (in case the object is duplicated or reloaded)
        Transform existingRoot = transform.Find("BarricadeVisuals");
        if (existingRoot != null)
        {
            visualRoot = existingRoot;
        }
        else
        {
            visualRoot = new GameObject("BarricadeVisuals").transform;
            visualRoot.SetParent(transform);
            visualRoot.localPosition = Vector3.zero;
            visualRoot.localRotation = Quaternion.identity;
        }

        calculateBarricadeHp();
        visualizeBarricade();
    }

    void Update()
    {
        if (debugMode) DebugControls();
    }

    void DebugControls()
    {
        int newLevel = barricadeLevel;

        if (debugLevel0) newLevel = 0;
        if (debugLevel1) newLevel = 1;
        if (debugLevel2) newLevel = 2;
        if (debugLevel3) newLevel = 3;
        if (debugLevel4) newLevel = 4;
        if (debugLevel5) newLevel = 5;
        if (debugLevel6) newLevel = 6;
        if (debugLevel7) newLevel = 7;
        if (debugLevel8) newLevel = 8;
        if (debugLevel9) newLevel = 9;
        if (debugLevel10) newLevel = 10;

        if (newLevel != barricadeLevel)
        {
            barricadeLevel = newLevel;
            RefreshBarricade();
        }
    }

    void updateBarricadeHp()
    {
        //FUTURE ENEMY REFERENCE NEEDED HERE !!!!!!
        //if it touched the collider and its staet was attack take away certain hp based onattack power of the enemy
    }

    void calculateBarricadeHp()
    {
        switch (material)
        {
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
        // Deletion part
        while (visualRoot.childCount > 0)
        {
            DestroyImmediate(visualRoot.GetChild(0).gameObject);
        }

        if (material == Material.none || barricadeLevel <= 0)
            return;

        if (material == Material.wood)
            BuildWoodBarricade();
        else if (material == Material.metal)
            BuildMetalBarricade();
    }

    void BuildWoodBarricade()
    {
        float verticalSpacing = plankHeight*1.5f;
        int verticalCount = Mathf.Clamp(barricadeLevel, 0, 4);

        for (int i = 0; i < verticalCount; i++)
        {
            Vector3 pos = new Vector3(0, i * verticalSpacing, 0);
            Instantiate(plank, pos, Quaternion.identity, visualRoot);
        }

        if (barricadeLevel >= 5)
        {
            Vector3 pos = new Vector3(0, verticalCount * verticalSpacing, 0);

            GameObject crossPlank = Instantiate(plank, pos, Quaternion.identity, visualRoot);
            crossPlank.transform.localEulerAngles = new Vector3(0, 0, 45);
        }
    }

    void BuildMetalBarricade()
    {
        float verticalSpacing = metalSheetHeight;

        for (int i = 0; i < barricadeLevel; i++)
        {
            Vector3 pos = new Vector3(0, i * verticalSpacing, 0);
            Instantiate(metalSheet, pos, Quaternion.identity, visualRoot);
        }
    }

    public void RefreshBarricade()
    {
        calculateBarricadeHp();
        visualizeBarricade();
    }
}