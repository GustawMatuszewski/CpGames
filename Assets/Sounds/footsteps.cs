using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class FootstepsRedemption : MonoBehaviour
{
    public KCC playerLogic;

    [System.Serializable]
    public class MaterialGroup
    {
        public string namePart;
        public AudioClip[] clips;
    }

    public MaterialGroup[] surfaceGroups;
    public AudioClip defaultClip;

    [Header("Step Rate Settings")]
    public float walkStepRate = 0.5f;
    public float runStepRate = 0.3f;

    [Header("Audio Settings")]
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0f, 1f)] public float pitchVariance = 0.1f;
    public float raycastDistance = 1.5f;
    public float raycastOriginHeight = 0.5f;

    [Header("Debug")]
    public bool enableDebugLogs = false;

    private AudioSource audioSource;
    private float stepTimer;
    private int lastClipIndex = -1;

    private const int STATE_WALK = 3;
    private const int STATE_RUN = 4;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = volume;

        stepTimer = walkStepRate;
    }

    void Update()
    {
        if (playerLogic == null) return;

        int stateID = (int)playerLogic.state;
        bool isWalking = stateID == STATE_WALK;
        bool isRunning = stateID == STATE_RUN;
        bool isMoving = isWalking || isRunning;

        if (isMoving)
        {
            float currentRate = isRunning ? runStepRate : walkStepRate;
            stepTimer += Time.deltaTime;

            if (stepTimer >= currentRate)
            {
                PlayStep();
                stepTimer = 0f;
            }
        }
        else
        {
            stepTimer = walkStepRate;

            if (audioSource.isPlaying)
                audioSource.Stop();
        }
    }

    void PlayStep()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * raycastOriginHeight;

        if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, raycastDistance))
            return;

        string materialName = GetSurfaceName(hit);
        AudioClip clip = GetClip(materialName);

        if (clip == null) return;

        audioSource.pitch = Random.Range(1f - pitchVariance, 1f + pitchVariance);
        audioSource.clip = clip;
        audioSource.Play();

        if (enableDebugLogs)
            Debug.Log($"<color=yellow>HIT:</color> powierzchnia='{materialName}'");
    }

    
    string GetSurfaceName(RaycastHit hit)
    {
        
        Terrain terrain = hit.collider.GetComponent<Terrain>();
        if (terrain != null)
        {
            return GetDominantTerrainTexture(terrain, hit.point);
        }

      
        Renderer rend = hit.collider.GetComponent<Renderer>();
        if (rend == null) rend = hit.collider.GetComponentInParent<Renderer>();
        if (rend == null) rend = hit.collider.GetComponentInChildren<Renderer>();

        if (rend != null && rend.sharedMaterial != null)
            return rend.sharedMaterial.name.Replace(" (Instance)", "");

        if (hit.collider.sharedMaterial != null)
            return hit.collider.sharedMaterial.name;

        return hit.collider.gameObject.name;
    }


    string GetDominantTerrainTexture(Terrain terrain, Vector3 worldPos)
    {
        TerrainData data = terrain.terrainData;

       
        if (data.terrainLayers == null || data.terrainLayers.Length == 0)
        {
            if (enableDebugLogs) Debug.LogWarning("Terrain nie ma ¿adnych warstw tekstur!");
            return terrain.gameObject.name;
        }

        Vector3 terrainPos = terrain.transform.position;

        float normX = (worldPos.x - terrainPos.x) / data.size.x;
        float normZ = (worldPos.z - terrainPos.z) / data.size.z;

        int mapX = Mathf.Clamp(Mathf.FloorToInt(normX * data.alphamapWidth), 0, data.alphamapWidth - 1);
        int mapZ = Mathf.Clamp(Mathf.FloorToInt(normZ * data.alphamapHeight), 0, data.alphamapHeight - 1);

        float[,,] splatMap = data.GetAlphamaps(mapX, mapZ, 1, 1);

        int dominantIndex = 0;
        float maxWeight = 0f;
        int layerCount = Mathf.Min(splatMap.GetLength(2), data.terrainLayers.Length);

        for (int i = 0; i < layerCount; i++)
        {
            if (splatMap[0, 0, i] > maxWeight)
            {
                maxWeight = splatMap[0, 0, i];
                dominantIndex = i;
            }
        }

        TerrainLayer layer = data.terrainLayers[dominantIndex];

        
        if (layer == null || layer.diffuseTexture == null)
        {
            if (enableDebugLogs) Debug.LogWarning($"Warstwa {dominantIndex} nie ma tekstury!");
            return terrain.gameObject.name;
        }

        string texName = layer.diffuseTexture.name;

        if (enableDebugLogs)
            Debug.Log($"<color=lime>TERRAIN:</color> warstwa={dominantIndex} nazwa='{texName}' waga={maxWeight:F2}");

        return texName;
    }

    AudioClip GetClip(string matName)
    {
        if (string.IsNullOrEmpty(matName))
            return defaultClip;

        string lower = matName.ToLower();

        foreach (var group in surfaceGroups)
        {
            if (string.IsNullOrEmpty(group.namePart)) continue;
            if (!lower.Contains(group.namePart.ToLower())) continue;
            if (group.clips == null || group.clips.Length == 0) continue;

            return PickRandomClip(group.clips);
        }

        return defaultClip;
    }

    AudioClip PickRandomClip(AudioClip[] clips)
    {
        if (clips.Length == 1) return clips[0];

        int index;
        do { index = Random.Range(0, clips.Length); }
        while (index == lastClipIndex);

        lastClipIndex = index;
        return clips[index];
    }


}