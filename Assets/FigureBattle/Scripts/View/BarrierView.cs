using System.Collections;
using UnityEngine;

namespace FigureBattle
{
    // バリアの見た目。攻撃を受けるたびに小さく暗くなり、壊れると消える
    public class BarrierView : MonoBehaviour
    {
        public int Owner;
        public GridPos Pos;

        Transform crystal;
        Material mat;
        Color color;
        int maxHits;

        public void Init(int owner, GridPos pos, int hitsToBreak, Material template)
        {
            Owner = owner;
            Pos = pos;
            maxHits = Mathf.Max(1, hitsToBreak);
            color = Color.Lerp(ViewUtil.PlayerColor(owner), Color.white, 0.35f);
            mat = ViewUtil.NewMaterial(color, template);

            ViewUtil.Primitive(PrimitiveType.Cylinder, transform, new Vector3(0, 0.05f, 0), new Vector3(0.9f, 0.05f, 0.9f),
                ViewUtil.NewMaterial(ViewUtil.PlayerColor(owner) * 0.6f, template), "Base");
            crystal = ViewUtil.Primitive(PrimitiveType.Cube, transform, new Vector3(0, 0.7f, 0), new Vector3(0.5f, 0.5f, 0.5f), mat, "Crystal").transform;
            crystal.localRotation = Quaternion.Euler(45, 0, 45);

            var col = gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.6f, 0);
            col.size = new Vector3(0.9f, 1.2f, 0.9f);
        }

        void Update()
        {
            if (crystal != null)
                crystal.Rotate(0, 40f * Time.deltaTime, 0, Space.World);
        }

        public IEnumerator PlayHit(int hits)
        {
            float rest = 1f - (float)hits / maxHits;
            var target = Vector3.one * Mathf.Lerp(0.2f, 0.5f, rest);
            for (float t = 0; t < 0.3f; t += Time.deltaTime)
            {
                crystal.localScale = target * (1f + Mathf.Sin(t * 60f) * 0.15f);
                ViewUtil.SetColor(mat, Color.Lerp(Color.white, color * Mathf.Lerp(0.5f, 1f, rest), t / 0.3f));
                yield return null;
            }
            crystal.localScale = target;
        }

        public IEnumerator PlayBreak()
        {
            var from = crystal.localScale;
            for (float t = 0; t < 0.5f; t += Time.deltaTime)
            {
                crystal.localScale = Vector3.Lerp(from, Vector3.one * 0.9f, t / 0.5f);
                ViewUtil.SetColor(mat, Color.Lerp(color, Color.white, t / 0.5f));
                yield return null;
            }
            crystal.gameObject.SetActive(false);
        }
    }
}
