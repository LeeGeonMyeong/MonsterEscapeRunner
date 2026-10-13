using System.Collections.Generic;
using UnityEngine;

// Keep the existing component GUID so scene and prefab references remain intact.
public class ObstacleMover : MonoBehaviour
{
    public static readonly List<ObstacleMover> ActiveMonsters = new List<ObstacleMover>();
    [SerializeField] private float moveSpeed = 3f;
    public int Health { get; private set; } = 1;
    public int MaximumHealth { get; private set; } = 1;
    public bool IsResolved { get; private set; }
    public bool IsArmored => MaximumHealth > 1;
    public float CurrentMoveSpeed => GameManager.Instance != null ? GameManager.Instance.ObstacleSpeed : moveSpeed;
    private Transform visual;
    private SpriteRenderer artwork;
    private PlayerController player;
    private float flash;
    private readonly List<SpriteRenderer> healthPips = new List<SpriteRenderer>();
    private void Awake()
    {
        GetComponent<BoxCollider2D>().enabled = false;
        player = FindFirstObjectByType<PlayerController>();
        SpriteRenderer original = GetComponent<SpriteRenderer>();
        GameObject child = new GameObject("MonsterVisual");
        child.transform.SetParent(transform, false);
        visual = child.transform;
        artwork = child.AddComponent<SpriteRenderer>();
        artwork.sprite = CharacterArt.Monster != null ? CharacterArt.Monster : original.sprite;
        artwork.sortingOrder = 2;
        original.enabled = false;
        ConfigureHealth(1);
    }
    private void OnEnable() { ActiveMonsters.Add(this); }
    private void OnDisable() { ActiveMonsters.Remove(this); }
    public void ConfigureHealth(int health)
    {
        if (IsResolved) return;
        Health = MaximumHealth = Mathf.Clamp(health, 1, 2);
        foreach (SpriteRenderer pip in healthPips) if (pip != null) Destroy(pip.gameObject);
        healthPips.Clear();
        for (int i = 0; i < MaximumHealth; i++)
        {
            GameObject pip = new GameObject("MonsterHealth");
            pip.transform.SetParent(transform, false);
            pip.transform.localPosition = new Vector3((i - (MaximumHealth - 1) * 0.5f) * 0.25f, 0.75f, 0);
            pip.transform.localScale = new Vector3(0.2f, 0.08f, 1);
            SpriteRenderer renderer = pip.AddComponent<SpriteRenderer>();
            renderer.sprite = MonsterShot.Pixel;
            renderer.sortingOrder = 5;
            healthPips.Add(renderer);
        }
        RefreshAppearance();
    }
    private void RefreshAppearance()
    {
        if (artwork != null) artwork.color = IsArmored ? new Color(1f, 0.65f, 0.4f) : Color.white;
        for (int i = 0; i < healthPips.Count; i++)
            healthPips[i].color = i < Health ? new Color(1f, 0.35f, 0.3f) : new Color(0.15f, 0.17f, 0.22f);
    }
    private void Update()
    {
        if (IsResolved || GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;
        transform.position += Vector3.down * CurrentMoveSpeed * Time.deltaTime;
        flash = Mathf.Max(0, flash - Time.deltaTime);
        RefreshAppearance();
        if (flash > 0) artwork.color = Color.white;
        visual.localPosition = Vector3.up * Mathf.Sin(Time.time * 10f + transform.position.x) * 0.04f;
        visual.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 8f + transform.position.x) * 4f);
        // A monster in ANY lane that crosses the defense line counts as missed.
        if (player != null && transform.position.y < player.transform.position.y - 0.45f)
        {
            IsResolved = true;
            GameManager.Instance.RecordMiss();
            player.TakeMissDamage();
            Destroy(gameObject);
        }
    }
    public bool TakeDamage(int damage)
    {
        if (IsResolved || damage <= 0 || GameManager.Instance == null || !GameManager.Instance.IsPlaying) return false;
        Health = Mathf.Max(0, Health - damage);
        flash = 0.12f;
        RefreshAppearance();
        MonsterShot.Burst(transform.position, Health == 0 ? new Color(1f, 0.75f, 0.2f) : Color.white);
        if (Health == 0)
        {
            IsResolved = true;
            GameManager.Instance.RecordKill(IsArmored ? 90 : 50);
            Destroy(gameObject);
        }
        return true;
    }
}
