using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float laneDistance = 2f;
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private int maxHP = 3;
    [SerializeField] private Text hpText;
    [SerializeField] private float attackCooldown = 0.35f;
    [SerializeField] private float chargeDuration = 0.65f;
    public int CurrentHP { get; private set; }
    public int MaxHP => maxHP;
    public float LaneDistance => laneDistance;
    public bool IsCharging { get; private set; }
    public float ChargeProgress => IsCharging ? Mathf.Clamp01((Time.time - chargeStarted) / chargeDuration) : 0f;
    public bool CanAttack => GameManager.Instance != null && GameManager.Instance.IsPlaying && CurrentHP > 0;
    private Rigidbody2D body;
    private Transform visual;
    private int currentLane = 1;
    private float groundY, nextAttack, chargeStarted, attackFlash;
    private DamageFeedback feedback;
    private SpriteRenderer artwork;
    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        groundY = transform.position.y;
        CurrentHP = maxHP;
        SpriteRenderer source = GetComponent<SpriteRenderer>();
        GameObject child = new GameObject("PlayerVisual");
        child.transform.SetParent(transform, false);
        visual = child.transform;
        artwork = child.AddComponent<SpriteRenderer>();
        artwork.sprite = CharacterArt.Hero != null ? CharacterArt.Hero : source.sprite;
        artwork.color = Color.white;
        artwork.sortingLayerID = source.sortingLayerID;
        artwork.sortingOrder = source.sortingOrder;
        source.enabled = false;
        feedback = gameObject.AddComponent<DamageFeedback>();
        feedback.Initialize(artwork);
        UpdateHP();
    }
    private void Update()
    {
        if (!CanAttack) { CancelCharge(); return; }
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.leftArrowKey.wasPressedThisFrame) MoveLeft();
            if (keyboard.rightArrowKey.wasPressedThisFrame) MoveRight();
            if (keyboard.spaceKey.wasPressedThisFrame) Attack();
            if (keyboard.downArrowKey.wasPressedThisFrame) BeginCharge();
            if (keyboard.downArrowKey.wasReleasedThisFrame) ReleaseCharge();
        }
        attackFlash = Mathf.Max(0f, attackFlash - Time.deltaTime);
        visual.localPosition = Vector3.up * (Mathf.Sin(Time.time * 14f) * 0.035f + attackFlash * 0.6f) + feedback.Offset;
        visual.localRotation = Quaternion.Euler(0, 0, IsCharging ? Mathf.Sin(Time.time * 35f) * ChargeProgress * 5f : Mathf.Sin(Time.time * 7f) * 2f);
        visual.localScale = Vector3.one * (1f + ChargeProgress * 0.12f);
    }
    private void FixedUpdate()
    {
        if (!CanAttack) return;
        float x = Mathf.MoveTowards(body.position.x, (currentLane - 1) * laneDistance, moveSpeed * Time.fixedDeltaTime);
        body.MovePosition(new Vector2(x, groundY));
    }
    public void MoveLeft() { if (CanAttack) currentLane = Mathf.Max(0, currentLane - 1); }
    public void MoveRight() { if (CanAttack) currentLane = Mathf.Min(2, currentLane + 1); }
    public void Attack() { if (!IsCharging) Fire(1, false); }
    public void BeginCharge()
    {
        if (!CanAttack || IsCharging || Time.time < nextAttack) return;
        IsCharging = true;
        chargeStarted = Time.time;
    }
    public void ReleaseCharge()
    {
        if (!IsCharging) return;
        bool full = ChargeProgress >= 1f;
        IsCharging = false;
        Fire(full ? 2 : 1, full);
    }
    public void CancelCharge() { IsCharging = false; }
    private void Fire(int damage, bool piercing)
    {
        if (!CanAttack || Time.time < nextAttack) return;
        nextAttack = Time.time + attackCooldown;
        attackFlash = 0.2f;
        MonsterShot.Create(new Vector3(body.position.x, groundY + 0.65f, 0f), damage, piercing);
    }
    public void TakeMissDamage()
    {
        if (!CanAttack) return;
        CurrentHP = Mathf.Max(0, CurrentHP - 1);
        UpdateHP();
        feedback.Play(visual.position);
        int lostScore = GameManager.Instance.ApplyCollisionPenalty();
        GameManager.Instance.ShowDamage(visual.position, lostScore);
        if (CurrentHP == 0) GameManager.Instance.EndGame(false);
    }
    private void OnApplicationFocus(bool focused) { if (!focused) CancelCharge(); }
    private void OnDisable() { CancelCharge(); }
    private void UpdateHP()
    {
        if (hpText != null) hpText.text = "HP : " + CurrentHP;
        if (GameManager.Instance != null) GameManager.Instance.SetHealth(CurrentHP, maxHP);
    }
}
