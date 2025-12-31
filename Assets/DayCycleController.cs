using System.Collections;
using UnityEngine;

public class DayCycleController : MonoBehaviour
{
    public float fullCycleTime;
    public float dayTime;

    public float nightTime;
    public float currentTime;

    public Light sun;

    public float dayIntensity = 1f;
    public float nightIntensity = 0f;

    public bool cycleEnabled = true;
    bool isDay = true;

    public void Start(){
        nightTime = fullCycleTime - dayTime;
        StartCoroutine(TimeCycle());
    }

    public void EnableCycle(){
        cycleEnabled = true;
    }

    public void DisableCycle(){
        cycleEnabled = false;
    }

    IEnumerator TimeCycle(){
        while(true){
            while(cycleEnabled){
                float maxTime = isDay ? dayTime : nightTime;
                currentTime += Time.deltaTime;

                float t = currentTime / maxTime;

                if(isDay){
                    sun.transform.rotation = Quaternion.Euler(Mathf.Lerp(-10f, 170f, t), 0f, 0f);
                    sun.intensity = Mathf.Lerp(nightIntensity, dayIntensity, t);
                }else{
                    sun.transform.rotation = Quaternion.Euler(Mathf.Lerp(170f, 350f, t), 0f, 0f);
                    sun.intensity = Mathf.Lerp(dayIntensity, nightIntensity, t);
                }

                if(currentTime >= maxTime){
                    currentTime = 0f;
                    isDay = !isDay;
                }

                yield return null;
            }

            yield return null;
        }
    }
}
