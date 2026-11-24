using UnityEngine;

public class XpGem : MonoBehaviour
{
    [SerializeField] int xpValue = 1;
    [SerializeField] float magnetSpeed = 8f;
    [SerializeField] float collectDistance = 0.3f;
    bool attracted;
    private void Update()
    {
        PlayerXP player = PlayerXP.Instance;
        if (player == null) return;
        Vector2 toPlayer = (Vector2)player.transform.position - (Vector2)transform.position;
        float sqrDist = toPlayer.sqrMagnitude;
        if (!attracted && sqrDist <= player.PickupRange * player.PickupRange)
            attracted = true;
        if (!attracted) return;
        if (sqrDist <= collectDistance * collectDistance)
        {
            player.AddXp(xpValue);
            Destroy(gameObject);
            return;
        }
        transform.position += (Vector3)(toPlayer.normalized * magnetSpeed * Time.deltaTime);
    }
}