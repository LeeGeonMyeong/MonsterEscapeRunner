using UnityEngine;

public class RunnerItemSpawner : MonoBehaviour
{
    private float nextAt = 6f;
    private int spawned;
    public void ResetRun() { nextAt = 6f; spawned = 0; RunnerItem.Clear(); }
    private void Update()
    {
        GameManager manager = GameManager.Instance;
        if (manager == null || !manager.IsPlaying || manager.ElapsedTime < nextAt) return;
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player == null) return;
        RunnerItem.Kind kind = spawned % 2 == 0 ? RunnerItem.Kind.Heal : RunnerItem.Kind.Bonus;
        RunnerItem.Create(kind, new Vector3((Random.Range(0, 3) - 1) * player.LaneDistance, 6f, 0f));
        spawned++;
        nextAt += 8f;
    }
}
