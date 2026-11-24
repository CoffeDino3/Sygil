using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerDeath : MonoBehaviour
{
    Health health;
    void Awake()
    {
        health = GetComponent<Health>();
        health.OnDied += HandleDeath;
    }
    private void OnDestroy()
    {
        health.OnDied -= HandleDeath;
    }
    void HandleDeath()
    {
        Debug.Log("Game over");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}