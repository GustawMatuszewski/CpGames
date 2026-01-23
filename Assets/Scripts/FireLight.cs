using UnityEngine;
using System.Collections.Generic;

public class FireLight : MonoBehaviour
{
    public Light fireLight;

    public float baseIntensity = 1.5f;
    public float intensityVariation = 0.5f;
    public float flickerSpeed = 15f;

    public List<Color> fireColors = new List<Color>();

    public float colorBlendSpeed = 2f;

    float noiseOffset;
    int currentColorIndex;
    int nextColorIndex;
    float colorT;

    public void Start(){
        noiseOffset = Random.Range(0f, 1000f);

        if(fireColors.Count < 2){
            fireColors.Add(new Color(1f, 0.6f, 0.2f));
            fireColors.Add(new Color(1f, 0.4f, 0.1f));
        }

        currentColorIndex = 0;
        nextColorIndex = 1;
    }

    public void Update(){
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, noiseOffset);
        fireLight.intensity = baseIntensity + (noise - 0.5f) * intensityVariation;

        colorT += Time.deltaTime * colorBlendSpeed;
        if(colorT >= 1f){
            colorT = 0f;
            currentColorIndex = nextColorIndex;
            nextColorIndex = Random.Range(0, fireColors.Count);
            if(nextColorIndex == currentColorIndex){
                nextColorIndex = (nextColorIndex + 1) % fireColors.Count;
            }
        }

        fireLight.color = Color.Lerp(
            fireColors[currentColorIndex],
            fireColors[nextColorIndex],
            colorT
        );
    }
}
