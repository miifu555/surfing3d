using UnityEngine;

// シーンごとの BGM を鳴らす。GameSystems などに付けて使う。
// タイトルとゲームで別の曲を鳴らしたいので、シーンをまたいでは残さない。
[RequireComponent(typeof(AudioSource))]
public class BgmPlayer : MonoBehaviour
{
    [Header("音源")]
    // ここに BGM の AudioClip を入れる
    public AudioClip bgm;
    [Range(0.0f, 1.0f)]
    public float volume = 0.5f;
    public bool loop = true;
    // シーンが始まったら自動で鳴らす
    public bool playOnStart = true;

    [Header("フェード")]
    // 鳴り始めにこの秒数かけて音量を上げる。0 なら最初から最大音量
    public float fadeInDuration = 0.8f;

    private AudioSource source;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0.0f;   // 距離で音量が変わらないよう 2D
    }

    void Start()
    {
        if (playOnStart)
        {
            Play();
        }
    }

    public void Play()
    {
        if (bgm == null) return;

        source.clip = bgm;
        source.loop = loop;
        source.volume = (fadeInDuration > 0.0f) ? 0.0f : volume;
        source.Play();
    }

    public void Stop()
    {
        source.Stop();
    }

    void Update()
    {
        if (fadeInDuration <= 0.0f) return;
        if (source.isPlaying == false) return;
        if (source.volume >= volume) return;

        // timeScale の影響を受けないようにしておく
        float rate = volume / fadeInDuration;
        source.volume = Mathf.MoveTowards(source.volume, volume, rate * Time.unscaledDeltaTime);
    }
}
