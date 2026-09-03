using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 게임 효과음을 중앙에서 재생합니다.
/// Inspector의 클립 목록만 교체하면 사운드를 변경할 수 있습니다.
/// </summary>
public sealed class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Output")]
    [SerializeField] private AudioMixerGroup _uiMixerGroup;
    [SerializeField] private AudioMixerGroup _sfxMixerGroup;
    [SerializeField, Range(0f, 1f)] private float _uiVolume = 0.8f;
    [SerializeField, Range(0f, 1f)] private float _sfxVolume = 0.7f;
    [SerializeField, Range(0f, 1f)] private float _musicVolume = 0.45f;

    [Header("Background Music")]
    [Tooltip("기본 파일: Music/Menu Music.mp3")]
    [SerializeField] private AudioClip _menuMusicClip;
    [Tooltip("기본 파일: Music/Game Music.mp3")]
    [SerializeField] private AudioClip _gameMusicClip;

    [Header("Menu Selection")]
    [Tooltip("기본 후보: UI/ui_sound_forward.mp3, UI/ui_sound_back.mp3")]
    [SerializeField] private AudioClip[] _menuSelectionClips;

    [Header("Tower Attack")]
    [Tooltip("기본 후보: SFX/Towers/MachineGun/MG 1.wav ~ MG 14.wav")]
    [SerializeField] private AudioClip[] _towerAttackClips;

    [Header("Tower Upgrade")]
    [Tooltip("기본 후보: UI/TD Tower Upgrade.wav")]
    [SerializeField] private AudioClip _towerUpgradeClip;

    [Header("Tower Construction and Sale")]
    [Tooltip("기본 파일: SFX/Building.wav")]
    [SerializeField] private AudioClip _towerConstructionClip;
    [Tooltip("기본 파일: UI/TD Tower Sell.wav")]
    [SerializeField] private AudioClip _towerSellClip;

    [Header("Monster Death")]
    [Tooltip("기본 후보: SFX/Enemies Exploding/Enemies Exploding 1.wav ~ 5.wav")]
    [SerializeField] private AudioClip[] _monsterDeathClips;

    [Header("Game Result and Escape")]
    [Tooltip("기본 파일: SFX/Base Attack/zone_enter.wav")]
    [SerializeField] private AudioClip _monsterReachedGoalClip;
    [Tooltip("기본 파일: UI/TD Defeat .wav")]
    [SerializeField] private AudioClip _defeatClip;
    [Tooltip("기본 파일: UI/TD Victory.wav")]
    [SerializeField] private AudioClip _victoryClip;

    private AudioSource _uiSource;
    private AudioSource _sfxSource;
    private AudioSource _musicSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _uiSource = CreateSource("UI Audio", _uiMixerGroup, _uiVolume);
        _sfxSource = CreateSource("SFX Audio", _sfxMixerGroup, _sfxVolume);
        _musicSource = CreateSource("Background Music", _sfxMixerGroup, _musicVolume);
        _musicSource.loop = true;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void PlayMenuSelection() { PlayRandom(_uiSource, _menuSelectionClips); }
    public void PlayTowerAttack() { PlayRandom(_sfxSource, _towerAttackClips); }
    public void PlayTowerUpgrade() { Play(_uiSource, _towerUpgradeClip); }
    public void PlayTowerConstruction() { Play(_sfxSource, _towerConstructionClip); }
    public void PlayTowerSell() { Play(_uiSource, _towerSellClip); }
    public void PlayMonsterDeath() { PlayRandom(_sfxSource, _monsterDeathClips); }
    public void PlayMonsterReachedGoal() { Play(_sfxSource, _monsterReachedGoalClip); }
    public void PlayDefeat() { Play(_uiSource, _defeatClip); }
    public void PlayVictory() { Play(_uiSource, _victoryClip); }
    public void PlayMenuMusic() { PlayMusic(_menuMusicClip); }
    public void PlayGameMusic() { PlayMusic(_gameMusicClip); }

    private void PlayMusic(AudioClip clip)
    {
        if (_musicSource == null || clip == null || _musicSource.clip == clip && _musicSource.isPlaying)
        {
            return;
        }

        _musicSource.Stop();
        _musicSource.clip = clip;
        _musicSource.Play();
    }

    private AudioSource CreateSource(string sourceName, AudioMixerGroup group, float volume)
    {
        GameObject sourceObject = new GameObject(sourceName);
        sourceObject.transform.SetParent(transform, false);
        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = volume;
        source.outputAudioMixerGroup = group;
        return source;
    }

    private static void Play(AudioSource source, AudioClip clip)
    {
        if (source != null && clip != null) source.PlayOneShot(clip);
    }

    private static void PlayRandom(AudioSource source, AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return;
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        Play(source, clip);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        _menuMusicClip = LoadClip("Assets/Resource/Audio/Music/Menu Music.mp3");
        _gameMusicClip = LoadClip("Assets/Resource/Audio/Music/Game Music.mp3");
        _towerUpgradeClip = LoadClip("Assets/Resource/Audio/UI/TD Tower Upgrade.wav");
        _towerConstructionClip = LoadClip("Assets/Resource/Audio/SFX/Building.wav");
        _towerSellClip = LoadClip("Assets/Resource/Audio/UI/TD Tower Sell.wav");
        _monsterReachedGoalClip = LoadClip("Assets/Resource/Audio/SFX/Base Attack/zone_enter.wav");
        _defeatClip = LoadClip("Assets/Resource/Audio/UI/TD Defeat .wav");
        _victoryClip = LoadClip("Assets/Resource/Audio/UI/TD Victory.wav");
        _menuSelectionClips = new[]
        {
            LoadClip("Assets/Resource/Audio/UI/ui_sound_forward.mp3"),
            LoadClip("Assets/Resource/Audio/UI/ui_sound_back.mp3")
        };
        _towerAttackClips = new[]
        {
            LoadClip("Assets/Resource/Audio/SFX/Towers/MachineGun/MG 1.wav"),
            LoadClip("Assets/Resource/Audio/SFX/Towers/MachineGun/MG 2.wav"),
            LoadClip("Assets/Resource/Audio/SFX/Towers/MachineGun/MG 3.wav")
        };
        _monsterDeathClips = new[]
        {
            LoadClip("Assets/Resource/Audio/SFX/Enemies Exploding/Enemies Exploding 1.wav"),
            LoadClip("Assets/Resource/Audio/SFX/Enemies Exploding/Enemies Exploding 2.wav"),
            LoadClip("Assets/Resource/Audio/SFX/Enemies Exploding/Enemies Exploding 3.wav"),
            LoadClip("Assets/Resource/Audio/SFX/Enemies Exploding/Enemies Exploding 4.wav"),
            LoadClip("Assets/Resource/Audio/SFX/Enemies Exploding/Enemies Exploding 5.wav")
        };
    }

    private static AudioClip LoadClip(string path)
    {
        return UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }
#endif
}
