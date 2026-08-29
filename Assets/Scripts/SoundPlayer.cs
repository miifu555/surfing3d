using UnityEngine;

// 効果音を鳴らすための共有プレイヤー。
//
// 宝石は取った瞬間に非表示になるため、宝石自身に AudioSource を置くと
// 鳴り始めた音が途中で切れてしまう。そこでシーンに1つだけ AudioSource を
// 用意して、そこから鳴らす。最初に呼ばれたときに自動で作られるので、
// シーン側の準備は不要（AudioListener だけあればよい）。
// シーンを移っても消えないので、ボタンのクリック音も最後まで鳴る。
public static class SoundPlayer
{
    static AudioSource source;

    // clip が未設定なら何もしない
    public static void Play(AudioClip clip, float volume, float pitchVariation)
    {
        if (clip == null) return;

        AudioSource target = GetSource();
        if (target == null) return;

        // 連続で取ったときに同じ音が重なって耳障りにならないよう、少し音程をずらす
        target.pitch = (pitchVariation > 0.0f)
            ? 1.0f + Random.Range(-pitchVariation, pitchVariation)
            : 1.0f;

        target.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    static AudioSource GetSource()
    {
        // Unity の == は破棄済みオブジェクトを null 扱いにするので、
        // 万一消えていても作り直せる
        if (source != null) return source;

        GameObject go = new GameObject("SoundPlayer");
        // ボタンのクリック音のようにシーンを切り替える直前に鳴らす音が
        // 読み込みで途切れないよう、シーンをまたいで残す
        Object.DontDestroyOnLoad(go);

        source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        // 距離で音量が変わらないよう 2D で鳴らす
        source.spatialBlend = 0.0f;

        return source;
    }
}
