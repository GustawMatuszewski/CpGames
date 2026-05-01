using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(AudioSource))]
public class FootstepsRedemption : MonoBehaviour
{
    [Header("Player Logic (zostaw puste dla NPC)")]
    public KCC playerLogic;

    [System.Serializable]
    public class MaterialGroup
    {
        public string namePart;
        public AudioClip[] clips;
    }

    public MaterialGroup[] surfaceGroups;
    public AudioClip defaultClip;

    [Header("Landing")]
    public AudioClip landingClip;
    [Range(0f, 2f)] public float landingVolumeMultiplier = 1.5f;

    [Header("Step Rate Settings")]
    public float walkStepRate = 0.5f;
    public float runStepRate = 0.3f;
    public float sprintStepRate = 0.2f;

    [Header("Audio Settings")]
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0f, 0.5f)] public float pitchVariance = 0.2f;

    [Header("3D Audio / Odleglosc")]
    public float minDistance = 1f;
    public float maxDistance = 20f;
    [Range(0f, 1f)] public float spatialBlend = 1f;

    [Header("NPC Detection")]
    public float moveThreshold = 0.1f;

    [Header("Raycast Settings")]
    public float raycastDistance = 1.5f;
    public float raycastOriginHeight = 0.5f;

    [Header("Debug")]
    public bool enableDebugLogs = false;

    private AudioSource audioSource;
    private float stepTimer;
    private int lastClipIndex = -1;
    private bool pitchGoingUp = false;
    private bool wasInAir = false;
    private Rigidbody rb;
    private NavMeshAgent navAgent;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.volume = volume;
        audioSource.pitch = 1f;
        audioSource.spatialBlend = spatialBlend;
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;
        audioSource.rolloffMode = AudioRolloffMode.Linear;

        rb = GetComponent<Rigidbody>();
        navAgent = GetComponent<NavMeshAgent>();
        stepTimer = walkStepRate;
    }

    void Update()
    {
        if (playerLogic != null)
        {
            UpdateKCC();
        }
        else if (navAgent != null)
        {
            UpdateNavMesh();
        }
        else if (rb != null)
        {
            UpdateRigidbody();
        }
    }

    void UpdateKCC()
    {
        bool isAir = playerLogic.state == KCC.State.Air;
        bool isWalking = playerLogic.state == KCC.State.Walk;
        bool isRunning = playerLogic.state == KCC.State.Run;
        bool isSprinting = playerLogic.state == KCC.State.Sprint;
        bool isMoving = isWalking || isRunning || isSprinting;

        if (wasInAir && !isAir)
            PlayLanding();
        wasInAir = isAir;

        if (isMoving)
        {
            float currentRate = isSprinting ? sprintStepRate : isRunning ? runStepRate : walkStepRate;
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

            if (!isAir && audioSource.isPlaying && audioSource.clip != landingClip)
                audioSource.Stop();
        }
    }

    void UpdateNavMesh()
    {
        bool isMoving = navAgent.velocity.magnitude > moveThreshold;

        if (isMoving)
        {
            stepTimer += Time.deltaTime;

            if (stepTimer >= walkStepRate)
            {
                PlayStep();
                stepTimer = 0f;
            }
        }
        else
        {
            stepTimer = walkStepRate;

            if (audioSource.isPlaying && audioSource.clip != landingClip)
                audioSource.Stop();
        }
    }

    void UpdateRigidbody()
    {
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        bool isMoving = horizontalVelocity.magnitude > moveThreshold;
        bool isAir = !IsGroundedRb();

        if (wasInAir && !isAir)
            PlayLanding();
        wasInAir = isAir;

        if (isMoving && !isAir)
        {
            stepTimer += Time.deltaTime;

            if (stepTimer >= walkStepRate)
            {
                PlayStep();
                stepTimer = 0f;
            }
        }
        else
        {
            stepTimer = walkStepRate;

            if (!isAir && audioSource.isPlaying && audioSource.clip != landingClip)
                audioSource.Stop();
        }
    }

    bool IsGroundedRb()
    {
        return Physics.Raycast(
            transform.position + Vector3.up * 0.1f,
            Vector3.down,
            0.3f
        );
    }

    void PlayLanding()
    {
        if (landingClip == null) return;

        float newPitch = GetNewPitch();
        audioSource.pitch = newPitch;
        audioSource.volume = Mathf.Clamp01(volume * landingVolumeMultiplier);
        audioSource.clip = landingClip;
        audioSource.Play();

        Invoke(nameof(RestoreVolume), landingClip.length);

        if (enableDebugLogs)
            Debug.Log($"<color=cyan>LANDING:</color> pitch={newPitch:F2}");
    }

    void RestoreVolume()
    {
        audioSource.volume = volume;
    }

    void PlayStep()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * raycastOriginHeight;

        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, raycastDistance);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        RaycastHit validHit = default;
        bool found = false;

        foreach (var hit in hits)
        {
            if (hit.collider.transform.IsChildOf(transform)) continue;
            if (hit.collider.transform == transform) continue;

            validHit = hit;
            found = true;
            break;
        }

        if (!found) return;

        string materialName = GetSurfaceName(validHit);
        AudioClip clip = GetClip(materialName);

        if (clip == null) return;

        float newPitch = GetNewPitch();
        audioSource.pitch = newPitch;
        audioSource.volume = volume;
        audioSource.clip = clip;
        audioSource.Play();

        if (enableDebugLogs)
            Debug.Log($"<color=yellow>HIT:</color> powierzchnia='{materialName}' pitch={newPitch:F2}");
    }

    float GetNewPitch()
    {
        pitchGoingUp = !pitchGoingUp;
        float baseShift = pitchVariance * 0.6f;
        float randomOffset = Random.Range(0f, pitchVariance * 0.4f);
        float newPitch = pitchGoingUp
            ? 1f + baseShift + randomOffset
            : 1f - baseShift - randomOffset;

        newPitch += Random.Range(-0.02f, 0.02f);
        return Mathf.Clamp(newPitch, 0.7f, 1.3f);
    }

    string GetSurfaceName(RaycastHit hit)
    {
        Terrain terrain = hit.collider.GetComponent<Terrain>();
        if (terrain != null)
            return GetDominantTerrainTexture(terrain, hit.point);

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

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 origin = transform.position + Vector3.up * raycastOriginHeight;
        Gizmos.DrawLine(origin, origin + Vector3.down * raycastDistance);
        Gizmos.DrawWireSphere(origin + Vector3.down * raycastDistance, 0.05f);

        Gizmos.color = new Color(1f, 1f, 0f, 0.1f);
        Gizmos.DrawWireSphere(transform.position, minDistance);
        Gizmos.color = new Color(1f, 0f, 0f, 0.05f);
        Gizmos.DrawWireSphere(transform.position, maxDistance);
    }
#endif
}