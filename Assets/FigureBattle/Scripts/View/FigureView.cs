using System.Collections;
using UnityEngine;

namespace FigureBattle
{
    // フィギュアの見た目。モデルが無ければクラスごとの仮の形で作る
    public class FigureView : MonoBehaviour
    {
        public FigureUnit Unit { get; private set; }

        Transform body;
        Transform rarityRing;
        Material bodyMat;
        Color bodyColor;
        ActState lastState = (ActState)(-1);

        public void Init(FigureUnit unit, Material template)
        {
            Unit = unit;
            name = $"P{unit.Owner + 1}_{unit.Spec.figureName}";

            // 台座：プレイヤーの色
            ViewUtil.Primitive(PrimitiveType.Cylinder, transform, new Vector3(0, 0.04f, 0), new Vector3(0.75f, 0.04f, 0.75f),
                ViewUtil.NewMaterial(ViewUtil.PlayerColor(unit.Owner), template), "Base");
            // レアリティの輪。レアリティが高いほど派手に回る
            rarityRing = ViewUtil.Primitive(PrimitiveType.Cylinder, transform, new Vector3(0, 0.02f, 0), new Vector3(0.88f, 0.02f, 0.88f),
                ViewUtil.NewMaterial(ViewUtil.RarityColor(unit.Spec.rarity), template), "RarityRing").transform;

            bodyColor = ViewUtil.ClassColor(unit.Spec.figureClass);
            bodyMat = ViewUtil.NewMaterial(bodyColor, template);

            if (unit.Spec.modelPrefab != null)
            {
                var model = Instantiate(unit.Spec.modelPrefab, transform);
                model.transform.localPosition = new Vector3(0, 0.08f, 0);
                foreach (var c in model.GetComponentsInChildren<Collider>()) Destroy(c);
                body = model.transform;
            }
            else
            {
                body = new GameObject("Body").transform;
                body.SetParent(transform, false);
                BuildPlaceholder(unit.Spec.figureClass, body);
            }

            if (unit.Owner == 1) transform.rotation = Quaternion.Euler(0, 180, 0);

            var col = gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.5f, 0);
            col.size = new Vector3(0.8f, 1f, 0.8f);
        }

        void BuildPlaceholder(FigureClass cls, Transform parent)
        {
            switch (cls)
            {
                case FigureClass.Soldier:
                    ViewUtil.Primitive(PrimitiveType.Capsule, parent, new Vector3(0, 0.45f, 0), new Vector3(0.4f, 0.35f, 0.4f), bodyMat);
                    ViewUtil.Primitive(PrimitiveType.Cube, parent, new Vector3(0.25f, 0.5f, 0.15f), new Vector3(0.06f, 0.6f, 0.06f), bodyMat, "Sword");
                    break;
                case FigureClass.Sniper:
                    ViewUtil.Primitive(PrimitiveType.Cylinder, parent, new Vector3(0, 0.4f, 0), new Vector3(0.3f, 0.32f, 0.3f), bodyMat);
                    ViewUtil.Primitive(PrimitiveType.Cylinder, parent, new Vector3(0, 0.6f, 0.25f), new Vector3(0.06f, 0.3f, 0.06f), bodyMat, "Barrel")
                        .transform.localRotation = Quaternion.Euler(90, 0, 0);
                    break;
                case FigureClass.Jet:
                    ViewUtil.Primitive(PrimitiveType.Sphere, parent, new Vector3(0, 0.45f, 0), new Vector3(0.35f, 0.3f, 0.55f), bodyMat);
                    ViewUtil.Primitive(PrimitiveType.Cube, parent, new Vector3(0, 0.45f, -0.05f), new Vector3(0.85f, 0.05f, 0.22f), bodyMat, "Wing");
                    break;
                case FigureClass.Breaker:
                    ViewUtil.Primitive(PrimitiveType.Cube, parent, new Vector3(0, 0.38f, 0), new Vector3(0.6f, 0.6f, 0.55f), bodyMat);
                    ViewUtil.Primitive(PrimitiveType.Cube, parent, new Vector3(0, 0.78f, 0), new Vector3(0.3f, 0.22f, 0.3f), bodyMat, "Head");
                    break;
            }
        }

        void Update()
        {
            if (Unit == null) return;
            int spin = (int)Unit.Spec.rarity;
            if (spin >= (int)Rarity.SR)
                rarityRing.localScale = new Vector3(0.88f, 0.02f, 0.88f) * (1f + Mathf.Sin(Time.time * (spin + 1)) * 0.06f);

            // 行動を終えたフィギュアは暗くする
            if (Unit.State != lastState)
            {
                lastState = Unit.State;
                bool dim = Unit.State == ActState.Done;
                ViewUtil.SetColor(bodyMat, dim ? bodyColor * 0.45f : bodyColor);
            }
        }

        public IEnumerator Appear(Vector3 pos)
        {
            gameObject.SetActive(true);
            transform.position = pos;
            for (float t = 0; t < 0.3f; t += Time.deltaTime)
            {
                transform.localScale = Vector3.one * Mathf.SmoothStep(0f, 1f, t / 0.3f);
                yield return null;
            }
            transform.localScale = Vector3.one;
        }

        public IEnumerator MoveTo(Vector3 to)
        {
            var from = transform.position;
            float dur = Mathf.Clamp(Vector3.Distance(from, to) * 0.08f, 0.2f, 0.5f);
            for (float t = 0; t < dur; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / dur);
                transform.position = Vector3.Lerp(from, to, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.4f;
                yield return null;
            }
            transform.position = to;
        }

        public IEnumerator Lunge(Vector3 target)
        {
            var home = transform.position;
            var dir = target - home;
            dir.y = 0;
            var peak = home + dir.normalized * Mathf.Min(0.45f, dir.magnitude * 0.5f);
            for (float t = 0; t < 0.12f; t += Time.deltaTime) { transform.position = Vector3.Lerp(home, peak, t / 0.12f); yield return null; }
            for (float t = 0; t < 0.15f; t += Time.deltaTime) { transform.position = Vector3.Lerp(peak, home, t / 0.15f); yield return null; }
            transform.position = home;
        }

        public IEnumerator Shake()
        {
            var home = transform.position;
            for (float t = 0; t < 0.3f; t += Time.deltaTime)
            {
                transform.position = home + new Vector3(Mathf.Sin(t * 80f) * 0.08f, 0, 0);
                yield return null;
            }
            transform.position = home;
        }

        public IEnumerator Defeat()
        {
            for (float t = 0; t < 0.4f; t += Time.deltaTime)
            {
                float k = t / 0.4f;
                transform.localScale = Vector3.one * (1f - k);
                transform.Rotate(0, 720f * Time.deltaTime, 0);
                yield return null;
            }
            gameObject.SetActive(false);
            transform.localScale = Vector3.one;
            transform.rotation = Quaternion.Euler(0, Unit.Owner == 1 ? 180 : 0, 0);
        }
    }
}
