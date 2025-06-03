using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDController : MonoBehaviour
{
    [Header("References")]
    public Slider healthSlider;
    public TextMeshProUGUI ammoText;

    private PlayerHealth playerHealth;
    private WeaponManager weaponManager;

    public GameObject deathScreen;
    public TextMeshProUGUI countdownText;

    public void Initialize(GameObject player)
    {

        playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.onDeathStateChanged += HandleDeathScreen;
            playerHealth.onHealthChanged += UpdateHealthUI;
            HandleDeathScreen(playerHealth.isDead.Value);
            UpdateHealthUI(playerHealth.CurrentHealthNormalized);
        }
        else
        {
            Debug.LogError("HUDController: PlayerHealth not found!");
        }

        weaponManager = player.GetComponent<WeaponManager>();
        if (weaponManager != null)
        {
            weaponManager.OnAmmoChanged += UpdateAmmoUI;
            UpdateAmmoUI(weaponManager.CurrentAmmo, weaponManager.ReserveAmmo);
        }
        else
        {
            Debug.LogError("HUDController: WeaponManager not found!");
        }

    }

    void HandleDeathScreen(bool isDead)
    {
        if (deathScreen == null) return;

        deathScreen.SetActive(isDead);

        if (isDead)
        {
            countdown = playerHealth.RespawnDelay;

            if (countdownText == null)
                countdownText = deathScreen.GetComponentInChildren<TextMeshProUGUI>();
        }
    }

    void UpdateHealthUI(float normalizedHealth)
    {
        if (healthSlider != null)
            healthSlider.value = normalizedHealth;
    }

    void UpdateAmmoUI(int current, int reserve)
    {

        if (ammoText != null)
            ammoText.text = $"{current} / {reserve}";
        else
            Debug.LogError("HUDController: ammoText is NULL!");
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.onHealthChanged -= UpdateHealthUI;
        playerHealth.onDeathStateChanged -= HandleDeathScreen;

        if (weaponManager != null)
            weaponManager.OnAmmoChanged -= UpdateAmmoUI;
    }
    

    private float countdown;

    void Update()
    {
        if (deathScreen.activeSelf && countdown > 0f)
        {
            countdown -= Time.deltaTime;
            if (countdownText != null)
                countdownText.text = "Respawning in " + Mathf.CeilToInt(countdown) + "...";
        }
    }
}
