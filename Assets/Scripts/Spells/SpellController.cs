using UnityEngine;
//AccuracyCheck.cs

/*
0.00 - 0.35 = perfect
0.36 - 2.15 = very accurate
2.16 - 5.00 = poor
5.00+ = are you trying?

effectMultiplier = 1 / 1 (1 + accuracy);

Perfect; waterHeal = 100 * 0.3 * 1.1 = 22
Accurate; waterHeal = 100 * 0.3 * 0.75 = 15
Poor; waterHeal = 100 * 0.3 * 0.4 = 10
MISS; waterHeal = 100 * 0.3 * 0.1 = 2
*/

public class SpellController : MonoBehaviour
{
    public Patient patient;

    public bool readyToMerge;

    [Header("Water Spell")]
    public float waterPotency = 100f;
    public float waterDuration = 2f;
    public float testAccuracy = 0.38f;

    [Header("Wind Spell")]
    public float windPotency = 100f;
    public float windDuration = 2f;
    public float windTestAccuracy = 0.38f;

    [Header("Earth Spell")]
    public float earthPotency = 100f;
    public float earthDuration = 2f;
    public float earthTestAccuracy = 0.38f;

    [Header("Lightning Spell")]
    public float lightningPotency = 100f;
    public float lightningDuration = 2f;
    public float lightningTestAccuracy = 0.38f;

    [Header("Fire Spell")]
    public float firePotency = 100f;
    public float fireDuration = 2f;
    public float fireTestAccuracy = 0.38f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip waterSFX;

    void Update()
    //todo: implement later
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            CastWater(testAccuracy);
        }
            if (Input.GetKeyDown(KeyCode.Q))
        {
            CastWater(0.20f); // perfect
        }
        if (Input.GetKeyDown(KeyCode.W))
        {
            CastWater(0.38f); // v acc
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            CastWater(2.158401f); // poor
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            CastWater(5.5f); // are you trying
        }
    }

    public void CastWater(float acc)
    {
        if (patient == null) return;

        float mult = GetAccuracyMultiplier(acc);
        float totalHeal = calcWaterHeal(waterPotency, mult);

        float t = Mathf.InverseLerp(0.1f, 1.1f, mult);
        float reduction = Mathf.Lerp(0.6f, 0.3f, t);

        patient.startHoT(totalHeal, waterDuration);
        patient.bleedReduction(reduction, waterDuration); //reduces bleed based on accuracy of the spell


        if(audioSource != null && waterSFX != null)
        {
            audioSource.PlayOneShot(waterSFX);
        }

        Debug.Log(
            "water cast - acc: " + acc.ToString("F3") +
            " - multiplier: " + mult.ToString("F2") +
            " - total heal: " + totalHeal.ToString("F2") +
            " - bleed reduction: " + reduction.ToString("F2") 
        );
    }

    float GetAccuracyMultiplier(float acc)
    {
        if (acc <= 0.35f)
        {
            return 1.1f;
        }

        if (acc <= 2.15f)
        {
            return 0.75f;
        }

        if (acc <= 5f)
        {
            return 0.4f;
        }

        return 0.1f;
    }

    float calcWaterHeal(float pot, float mult)
    {
        return pot * 0.3f * mult;
    }
}
