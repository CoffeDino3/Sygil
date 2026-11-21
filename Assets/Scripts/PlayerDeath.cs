using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerDeath : MonoBehaviour
{
    private Health health;
    private void Awake()
    {
        health = GetComponent<Health>();
        health.OnDied += HandleDeath;
    }
    private void OnDestroy()
    {
        health.OnDied -= HandleDeath;
    }
    private void HandleDeath()
    {
        Debug.Log("Game over");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}