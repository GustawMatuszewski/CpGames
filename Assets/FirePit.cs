using UnityEngine;

public class FirePit : MonoBehaviour
{
    public Item fireStarter;
    public Item starterFuel;

    public float calories;
    public float caloriesBurnRate;
    public bool isLit = false;
    public bool attemptToLight = false;
    public float chanceToBeLit = 151;

    public float prepPoints = 0f;
    public float chanceToKeepStarter = 25f;

    void FixedUpdate(){
        if (attemptToLight){
            firePreping();
            fireStarting();

            if (fireStarter != null){
                float keepRoll = Random.Range(0f, 100f);
                if (keepRoll > chanceToKeepStarter)
                    fireStarter = null;
            }

            if (starterFuel != null)
                starterFuel = null;

            attemptToLight = false;
        }

        if (calories > 0 && isLit)
            calories -= caloriesBurnRate;
        else{
            isLit = false;
        }
    }

    void fireStarting(){
        bool canStartFromExisting = calories > 0;

        float roll = Random.Range(0, chanceToBeLit);

        if (roll <= prepPoints && fireStarter != null){
            isLit = true;

            if (starterFuel != null)
                calories = starterFuel.burnCalories;

            return;
        }

        if (roll <= prepPoints && canStartFromExisting && fireStarter != null){
            isLit = true;
        }
    }
    
    void firePreping(){
        if (fireStarter != null)
            prepPoints += fireStarter.burnCalories / 10;

        if (starterFuel != null)
            prepPoints += starterFuel.burnCalories / 10 / 4;

        prepPoints -= Random.Range(5f, 10f);
        prepPoints = Mathf.Clamp(prepPoints, 0f, 100f);
    }
}
