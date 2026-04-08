using UnityEngine;

/// <summary>
/// Spawns composite pinatas — grids of coloured squares that fall as one body.
/// Oscillates horizontally above the screen. Each square has independent health.
/// </summary>
public class SquareSpawner : MonoBehaviour
{
    [SerializeField] private float spawnInterval   = 1f;
    [SerializeField] private float squareSize      = 0.175f;
    [SerializeField] private float spawnY          = 7f;
    [SerializeField] private float gravityScale    = 0.4f;
    [SerializeField] private float maxHorizSpeed   = 0.5f;
    [SerializeField] private float maxAngularSpeed = 60f;
    [SerializeField] private float maxSpawnAngle   = 35f;
    [SerializeField] private int   gridWidth       = 5;
    [SerializeField] private int   gridHeight      = 5;
    [SerializeField] private float squareHealth    = 1f;
    [SerializeField] private float fieldWidth      = 6f;
    [SerializeField] private float oscillateSpeed  = 1f;

    private float  _timer;
    private Sprite _squareSprite;

    public float SquareSize => squareSize;
    public void SetGridSize(int w, int h) { gridWidth = w; gridHeight = h; }
    public void SetSpawnInterval(float interval) { spawnInterval = interval; }
    public void SetOscillateSpeed(float speed) { oscillateSpeed = speed; }
    public void SetFieldWidth(float width) { fieldWidth = width; }
    public void SetSquareHealth(float health) { squareHealth = health; }

    void Start()
    {
        _squareSprite = BuildSquareSprite();
        _timer = spawnInterval;
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer < spawnInterval) return;
        _timer = 0f;

        float pinataWidth  = gridWidth  * squareSize;
        float pinataHeight = gridHeight * squareSize;

        // Oscillate spawn x within field bounds
        float maxX = fieldWidth * 0.5f - pinataWidth * 0.5f;
        float spawnX = Mathf.Sin(Time.time * oscillateSpeed) * maxX;

        Vector2 spawnPos = new Vector2(spawnX, spawnY);

        if (Physics2D.OverlapBox(spawnPos, new Vector2(pinataWidth, pinataHeight), 0f) != null)
            return;

        SpawnPinata(spawnX);
    }

    void SpawnPinata(float spawnX)
    {
        var parent = new GameObject("Pinata");
        parent.transform.position = new Vector3(spawnX, spawnY, 0f);
        parent.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(-maxSpawnAngle, maxSpawnAngle));

        var rb = parent.AddComponent<Rigidbody2D>();
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.mass                   = 2f;
        rb.gravityScale           = gravityScale;
        rb.linearVelocity         = new Vector2(Random.Range(-maxHorizSpeed, maxHorizSpeed), 0f);
        rb.angularVelocity        = Random.Range(-maxAngularSpeed, maxAngularSpeed);

        var pinata = parent.AddComponent<Pinata>();
        var color  = Random.ColorHSV(0f, 1f, 0.35f, 0.55f, 0.95f, 1f);

        float offsetX = (gridWidth  - 1) * 0.5f;
        float offsetY = (gridHeight - 1) * 0.5f;

        for (int row = 0; row < gridHeight; row++)
        for (int col = 0; col < gridWidth; col++)
        {
            var sq = new GameObject("Square");
            sq.transform.SetParent(parent.transform, false);
            sq.transform.localPosition = new Vector3(
                (col - offsetX) * squareSize,
                (row - offsetY) * squareSize,
                0f);
            sq.transform.localScale = Vector3.one * squareSize;

            var sr = sq.AddComponent<SpriteRenderer>();
            sr.sprite       = _squareSprite;
            sr.color        = color;
            sr.sortingOrder = 1;

            sq.AddComponent<BoxCollider2D>();

            var ps = sq.AddComponent<PinataSquare>();
            ps.Init(pinata, squareHealth, col, row);
            pinata.Register(ps);
        }
    }

    static Sprite BuildSquareSprite()
    {
        const int texSize = 32;
        var tex    = new Texture2D(texSize, texSize);
        var pixels = new Color[texSize * texSize];
        for (int i = 0; i < pixels.Length; i++)
        {
            int x = i % texSize;
            int y = i / texSize;
            // 1-pixel darker border for outline
            if (x == 0 || x == texSize - 1 || y == 0 || y == texSize - 1)
                pixels[i] = new Color(0.6f, 0.6f, 0.6f, 1f);
            else
                pixels[i] = Color.white;
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, texSize, texSize),
                             new Vector2(0.5f, 0.5f), texSize);
    }
}
