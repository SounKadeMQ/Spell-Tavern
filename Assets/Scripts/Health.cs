using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private float maxHealth = 10f;
    private float currentHealth;


    //Commands for health info on an object with the script attached
    public float GetMaxHealth() => maxHealth;
    public float GetCurrentHealth() => currentHealth;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    //Method to change health of an object with the script attached
    public void ChangeHealth(float amount)
    {
        currentHealth += amount;

        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
        
        else if (currentHealth <= 0)
        {
            Destroy(gameObject);
        }
    }
}
