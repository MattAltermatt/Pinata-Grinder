using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Singleton that handles save/load to JSON on disk.
/// Auto-saves every 5 minutes. Manual save via Save().
/// Fires OnSaved so UI can show a brief indicator.
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private const float AutoSaveInterval = 300f; // 5 minutes
    private const string FileName = "save.json";

    private float _autoSaveTimer;

    public event Action OnSaved;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void Update()
    {
        _autoSaveTimer += Time.deltaTime;
        if (_autoSaveTimer >= AutoSaveInterval)
        {
            _autoSaveTimer = 0f;
            Save();
        }
    }

    public void Save()
    {
        var data = new SaveData();

        // Economy
        Economy.Instance.CaptureState(
            out data.money, out data.sawsPurchased,
            out data.stoppersPurchased, out data.lasersPurchased,
            out data.missilesPurchased);

        // Global upgrades
        if (GlobalUpgrades.Instance != null)
            data.globalUpgrades = GlobalUpgrades.Instance.CaptureState();

        // Stoppers
        var stoppers = Stopper.All;
        data.stoppers = new StopperSaveData[stoppers.Count];
        for (int i = 0; i < stoppers.Count; i++)
        {
            var s = stoppers[i];
            var sd = new StopperSaveData();
            sd.posX = s.transform.position.x;
            sd.posY = s.transform.position.y;
            sd.directionMultiplier = 1;

            if (s.HasWeapon)
            {
                sd.weaponType = (int)s.Weapon.Type;
                sd.upgradeLevels = s.Weapon.Upgrades.GetLevels();
                sd.totalInvestment = s.Weapon.Upgrades.TotalInvestment;

                if (s.Weapon is SawGroup saw)
                    sd.directionMultiplier = saw.IsClockwise ? -1 : 1;
            }
            else
            {
                sd.weaponType = (int)WeaponType.None;
            }

            data.stoppers[i] = sd;
        }

        string json = JsonUtility.ToJson(data);
        string path = GetSavePath();

        try
        {
            File.WriteAllText(path, json);
            SyncFileSystem();
        }
        catch (Exception e)
        {
            Debug.LogWarning("Save failed: " + e.Message);
            return;
        }

        OnSaved?.Invoke();
    }

    public void DeleteSave()
    {
        string path = GetSavePath();
        if (File.Exists(path))
            File.Delete(path);
    }

    public SaveData Load()
    {
        string path = GetSavePath();
        if (!File.Exists(path)) return null;

        try
        {
            string json = File.ReadAllText(path);
            var data = JsonUtility.FromJson<SaveData>(json);

            // Basic validity check
            if (data == null || data.stoppers == null || data.stoppers.Length == 0)
                return null;

            return data;
        }
        catch (Exception e)
        {
            Debug.LogWarning("Load failed (starting fresh): " + e.Message);
            return null;
        }
    }

    static string GetSavePath()
    {
        return Path.Combine(Application.persistentDataPath, FileName);
    }

    /// <summary>
    /// On WebGL, flush the virtual filesystem to IndexedDB so data persists
    /// across browser sessions.
    /// </summary>
    static void SyncFileSystem()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // Unity's Emscripten FS needs an explicit sync to persist to IndexedDB
        Application.ExternalEval("_JS_FileSystem_Sync();");
#endif
    }
}
