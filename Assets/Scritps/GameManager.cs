using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public enum RunState { Ready, Playing, Ended }
    public static GameManager Instance { get; private set; }
    [SerializeField] private float duration = 60f;
    [SerializeField] private Canvas canvas;
    [SerializeField] private Text hpText;
    [SerializeField] private float startingObstacleSpeed = 3f;
    [SerializeField] private float maximumObstacleSpeed = 5.5f;
    [SerializeField] private int collisionScorePenalty = 30;
    public RunState State { get; private set; } = RunState.Ready;
    public bool IsPlaying => State == RunState.Playing;
    public int CurrentScore => Mathf.Max(0, killScore - deductedScore);
    public float ElapsedTime => elapsed;
    public float DifficultyProgress => Mathf.Clamp01(elapsed / Mathf.Max(1f, duration));
    public int DifficultyLevel => Mathf.Min(4, Mathf.FloorToInt(DifficultyProgress * 4f) + 1);
    public float ObstacleSpeed => Mathf.Lerp(Mathf.Max(0.1f, startingObstacleSpeed),
        Mathf.Max(startingObstacleSpeed, maximumObstacleSpeed), DifficultyProgress);
    private float elapsed;
    private int deductedScore, killScore;
    public int Kills { get; private set; }
    public int Misses { get; private set; }
    private Text status, result;
    private GameObject gameOverPanel;
    private GameObject startPanel;
    private Font font;
    private Image healthFill, healthTrail, damageFlash;
    private RectTransform healthBar;
    private float healthTarget = 1f;
    private float flashTime;
    private Text levelNotice;
    private int displayedLevel = 1;
    private float noticeTime;
    private PlayerController player;
    private GameObject swipeArea, clickControls, holdControls;
    private Text controlsHelp;
    private Image chargeFill;
    private Text chargeLabel;
    private void Awake()
    {
        Instance = this;
        State = RunState.Ready;
        gameObject.AddComponent<RunnerBackground>();
        font = hpText.font;
        player = FindFirstObjectByType<PlayerController>();
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(480f, 854f);
        scaler.matchWidthOrHeight = 0.5f;
        Image hudBackdrop = MakeImage("HudBackdrop", canvas.transform, new Color(0.025f, 0.045f, 0.09f, 0.78f));
        RectTransform hudRect = hudBackdrop.rectTransform;
        hudRect.anchorMin = new Vector2(0f, 1f);
        hudRect.anchorMax = new Vector2(1f, 1f);
        hudRect.pivot = new Vector2(0.5f, 1f);
        hudRect.anchoredPosition = Vector2.zero;
        hudRect.sizeDelta = new Vector2(0f, 185f);
        hudBackdrop.transform.SetAsFirstSibling();
        Place(hpText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(460f, 45f));
        hpText.fontSize = 28;
        hpText.color = Color.white;
        hpText.alignment = TextAnchor.MiddleCenter;
        hpText.raycastTarget = false;
        BuildHealthBar();
        status = MakeText("RunStatus", canvas.transform, new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(500f, 80f), 25);
        levelNotice = MakeText("LevelNotice", canvas.transform, new Vector2(0.5f, 1f), new Vector2(0f, -215f), new Vector2(500f, 50f), 28);
        levelNotice.fontStyle = FontStyle.Bold;
        levelNotice.color = Color.clear;
        BuildPlayControls();
        gameOverPanel = new GameObject("GameOverPanel", typeof(RectTransform), typeof(Image));
        gameOverPanel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = (RectTransform)gameOverPanel.transform;
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
        gameOverPanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);
        result = MakeText("Result", gameOverPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), new Vector2(460f, 180f), 32);
        GameObject buttonObject = new GameObject("RestartButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(gameOverPanel.transform, false);
        Place((RectTransform)buttonObject.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -85f), new Vector2(280f, 80f));
        buttonObject.GetComponent<Image>().color = new Color(0.15f, 0.45f, 0.8f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = buttonObject.GetComponent<Image>();
        button.onClick.AddListener(Restart);
        Text label = MakeText("Label", buttonObject.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(280f, 80f), 30);
        label.text = "RESTART";
        gameOverPanel.SetActive(false);
        BuildStartScreen();
        RefreshStatus();
    }
    private void Update()
    {
        UpdateFeedback();
        if (!IsPlaying) return;
        elapsed = Mathf.Min(duration, elapsed + Time.deltaTime);
        RefreshStatus();
        if (elapsed >= duration) EndGame(true);
    }
    private void RefreshStatus()
    {
        status.text = "TIME : " + Mathf.CeilToInt(Mathf.Max(0f, duration - elapsed)) + "    SCORE : " + CurrentScore + "\nKILLS : " + Kills + "    LEVEL : " + DifficultyLevel;
        if (DifficultyLevel > displayedLevel)
        {
            displayedLevel = DifficultyLevel;
            noticeTime = 1.6f;
            levelNotice.text = "LEVEL " + displayedLevel + " - SPEED UP!";
        }
    }
    public void EndGame(bool survived)
    {
        if (!IsPlaying) return;
        State = RunState.Ended;
        result.text = (survived ? "SURVIVED!" : "GAME OVER") + "\nSCORE : " + CurrentScore + "\nKILLS : " + Kills + " / MISSED : " + Misses;
        swipeArea.SetActive(false);
        clickControls.SetActive(false);
        holdControls.SetActive(false);
        gameOverPanel.SetActive(true);
    }
    private void Restart() { SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
    private void OnDestroy() { if (Instance == this) Instance = null; }
    public void StartGame()
    {
        if (State != RunState.Ready) return;
        elapsed = 0f;
        deductedScore = 0;
        killScore = 0;
        Kills = Misses = 0;
        displayedLevel = 1;
        noticeTime = 0f;
        State = RunState.Playing;
        startPanel.SetActive(false);
        RefreshStatus();
    }
    public int ApplyCollisionPenalty()
    {
        if (!IsPlaying) return 0;
        // Only remove points already earned; low scores can recover immediately.
        int loss = Mathf.Min(CurrentScore, Mathf.Max(0, collisionScorePenalty));
        deductedScore += loss;
        RefreshStatus();
        return loss;
    }
    private void BuildStartScreen()
    {
        Image panel = MakeImage("StartPanel", canvas.transform, new Color(0.015f, 0.025f, 0.055f, 0.9f));
        panel.raycastTarget = true;
        startPanel = panel.gameObject;
        RectTransform rect = panel.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        Text title = MakeText("GameTitle", startPanel.transform, new Vector2(0.5f, 0.5f),
            new Vector2(0f, 205f), new Vector2(460f, 120f), 38);
        title.text = "MONSTER STRIKE\nRUNNER";
        title.fontStyle = FontStyle.Bold;
        title.color = new Color(0.45f, 1f, 0.85f);
        Text rules = MakeText("StartRules", startPanel.transform, new Vector2(0.5f, 0.5f),
            new Vector2(0f, 80f), new Vector2(460f, 120f), 21);
        rules.text = "Defeat monsters before they pass you\nSwipe: move / ATTACK: fire\nHold CHARGE, then release: power shot\nNormal: 1 HP / Orange: 2 HP\nMiss any lane: HP -1 / Score -30";
        rules.rectTransform.sizeDelta = new Vector2(460f, 180f);
        rules.rectTransform.anchoredPosition = new Vector2(0f, 30f);
        GameObject obj = new GameObject("StartButton", typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(startPanel.transform, false);
        Place((RectTransform)obj.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -170f), new Vector2(300f, 85f));
        Image background = obj.GetComponent<Image>();
        background.color = new Color(0.08f, 0.5f, 0.45f);
        Button button = obj.GetComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(StartGame);
        Text label = MakeText("StartLabel", obj.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 85f), 32);
        label.text = "START";
        label.fontStyle = FontStyle.Bold;
    }
    private void Start()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null) SetHealth(player.CurrentHP, player.MaxHP);
    }
    private void BuildPlayControls()
    {
        Image surface = MakeImage("SwipeArea", canvas.transform, Color.clear);
        swipeArea = surface.gameObject;
        surface.raycastTarget = true;
        surface.rectTransform.anchorMin = Vector2.zero;
        surface.rectTransform.anchorMax = Vector2.one;
        surface.rectTransform.offsetMin = surface.rectTransform.offsetMax = Vector2.zero;
        surface.transform.SetAsFirstSibling();
        surface.gameObject.AddComponent<RunnerSwipeInput>().player = player;
        clickControls = new GameObject("ClickControls", typeof(RectTransform));
        holdControls = new GameObject("HoldControls", typeof(RectTransform));
        foreach (GameObject group in new[] { clickControls, holdControls })
        {
            group.transform.SetParent(canvas.transform, false);
            RectTransform rect = (RectTransform)group.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        Button attack = MakeControlButton("AttackButton", clickControls.transform,
            new Vector2(-110f, 70f), new Vector2(170f, 85f), "ATTACK", new Vector2(0.5f, 0f));
        attack.onClick.AddListener(player.Attack);
        Button charge = MakeControlButton("ChargeButton", holdControls.transform,
            new Vector2(110f, 70f), new Vector2(170f, 85f), "HOLD CHARGE", new Vector2(0.5f, 0f));
        charge.enabled = false;
        charge.GetComponent<Image>().color = new Color(0.4f, 0.23f, 0.1f, 0.95f);
        charge.gameObject.AddComponent<RunnerHoldInput>().player = player;
        Image chargeTrack = MakeImage("ChargeTrack", holdControls.transform, new Color(0.12f, 0.13f, 0.2f));
        Place(chargeTrack.rectTransform, new Vector2(0.5f, 0f), new Vector2(110f, 124f), new Vector2(170f, 10f));
        chargeFill = MakeImage("ChargeFill", chargeTrack.transform, new Color(1f, 0.7f, 0.2f));
        Place(chargeFill.rectTransform, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(170f, 10f));
        chargeFill.rectTransform.pivot = new Vector2(0f, 0.5f);
        chargeFill.rectTransform.localScale = new Vector3(0f, 1f, 1f);
        chargeLabel = charge.GetComponentInChildren<Text>();
        controlsHelp = MakeText("Controls", canvas.transform, new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(450f, 24f), 16);
        controlsHelp.text = "Swipe to move / Space: attack / Down: charge";
    }
    private Button MakeControlButton(string name, Transform parent, Vector2 position, Vector2 size, string text, Vector2 anchor)
    {
        Image image = MakeImage(name, parent, new Color(0.08f, 0.35f, 0.4f, 0.9f));
        image.raycastTarget = true;
        Place(image.rectTransform, anchor, position, size);
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        Text label = MakeText("Label", image.transform, new Vector2(0.5f, 0.5f), Vector2.zero, size, 22);
        label.text = text;
        return button;
    }
    public void RecordKill(int points)
    {
        if (!IsPlaying) return;
        Kills++;
        killScore += Mathf.Max(0, points);
        RefreshStatus();
    }
    public void RecordMiss()
    {
        if (!IsPlaying) return;
        Misses++;
        noticeTime = 0.8f;
        levelNotice.text = "MONSTER ESCAPED!";
        RefreshStatus();
    }
    private void BuildHealthBar()
    {
        Image background = MakeImage("HealthBar", canvas.transform, new Color(0.07f, 0.09f, 0.16f, 0.95f));
        healthBar = background.rectTransform;
        Place(healthBar, new Vector2(0.5f, 1f), new Vector2(0f, -88f), new Vector2(280f, 22f));
        healthTrail = MakeImage("HealthLossTrail", healthBar, new Color(1f, 0.28f, 0.25f));
        healthFill = MakeImage("HealthFill", healthBar, new Color(0.25f, 0.95f, 0.65f));
        foreach (Image image in new[] { healthTrail, healthFill })
        {
            Place(image.rectTransform, new Vector2(0f, 0.5f), new Vector2(3f, 0f), new Vector2(274f, 16f));
            image.rectTransform.pivot = new Vector2(0f, 0.5f);
        }
        damageFlash = MakeImage("DamageFlash", canvas.transform, Color.clear);
        RectTransform rect = damageFlash.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
    public void SetHealth(int hp, int maximum)
    {
        healthTarget = maximum > 0 ? Mathf.Clamp01((float)hp / maximum) : 0f;
        hpText.text = "HP : " + hp + " / " + maximum;
    }
    public void ShowDamage(Vector3 worldPosition, int lostScore)
    {
        flashTime = 0.3f;
        damageFlash.transform.SetAsLastSibling();
        Camera camera = Camera.main;
        if (camera == null) return;
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform,
            camera.WorldToScreenPoint(worldPosition), null, out local);
        Text number = MakeText("DamageNumber", canvas.transform, new Vector2(0.5f, 0.5f),
            local + Vector2.up * 65f, new Vector2(120f, 70f), 42);
        number.text = "-1";
        number.fontStyle = FontStyle.Bold;
        number.color = new Color(1f, 0.4f, 0.35f);
        StartCoroutine(FloatDamageNumber(number));
        if (lostScore > 0)
        {
            Text scoreNumber = MakeText("ScorePenaltyNumber", canvas.transform, new Vector2(0.5f, 0.5f),
                local + Vector2.up * 115f, new Vector2(220f, 45f), 24);
            scoreNumber.text = "-" + lostScore + " SCORE";
            scoreNumber.fontStyle = FontStyle.Bold;
            scoreNumber.color = new Color(1f, 0.82f, 0.3f);
            StartCoroutine(FloatDamageNumber(scoreNumber));
        }
    }
    private void UpdateFeedback()
    {
        if (healthFill == null) return;
        chargeFill.rectTransform.localScale = new Vector3(player.ChargeProgress, 1f, 1f);
        chargeLabel.text = player.ChargeProgress >= 1f ? "RELEASE!" : "HOLD CHARGE";
        float delta = Time.unscaledDeltaTime;
        noticeTime = Mathf.Max(0f, noticeTime - delta);
        levelNotice.color = new Color(1f, 0.8f, 0.3f, Mathf.Clamp01(noticeTime / 0.4f));
        float fill = Mathf.MoveTowards(healthFill.rectTransform.localScale.x, healthTarget, delta * 4f);
        float trail = Mathf.MoveTowards(healthTrail.rectTransform.localScale.x, healthTarget, delta * 0.65f);
        healthFill.rectTransform.localScale = new Vector3(fill, 1f, 1f);
        healthTrail.rectTransform.localScale = new Vector3(trail, 1f, 1f);
        healthFill.color = healthTarget <= 0.34f ? new Color(1f, 0.35f, 0.3f) : new Color(0.25f, 0.95f, 0.65f);
        flashTime = Mathf.Max(0f, flashTime - delta);
        damageFlash.color = new Color(1f, 0.08f, 0.1f, flashTime / 0.3f * 0.22f);
        healthBar.localScale = Vector3.one * (1f + flashTime / 0.3f * 0.06f);
    }
    private IEnumerator FloatDamageNumber(Text number)
    {
        float age = 0f;
        Vector2 start = number.rectTransform.anchoredPosition;
        while (age < 0.65f)
        {
            age += Time.unscaledDeltaTime;
            number.rectTransform.anchoredPosition = start + Vector2.up * age * 90f;
            Color color = number.color;
            color.a = Mathf.Clamp01(1f - age / 0.65f);
            number.color = color;
            yield return null;
        }
        Destroy(number.gameObject);
    }
    private Image MakeImage(string name, Transform parent, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        Image image = obj.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }
    private Text MakeText(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, int fontSize)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        Text text = obj.GetComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
        Place(text.rectTransform, anchor, position, size);
        return text;
    }
    private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
