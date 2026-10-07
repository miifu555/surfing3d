using System.Collections.Generic;
using UnityEngine;

namespace FigureBattle
{
    // デッキ。好きなフィギュアを4つまで入れられる
    [CreateAssetMenu(menuName = "FigureBattle/Deck", fileName = "NewDeck")]
    public class DeckData : ScriptableObject
    {
        public const int MaxFigures = 4;

        public string deckName = "デッキ";
        public List<FigureData> figures = new List<FigureData>();

        public List<FigureSpec> ToSpecs()
        {
            var list = new List<FigureSpec>();
            foreach (var f in figures)
            {
                if (f == null) continue;
                list.Add(f.spec.Clone());
                if (list.Count >= MaxFigures) break;
            }
            return list;
        }

        void OnValidate()
        {
            if (figures.Count > MaxFigures)
            {
                Debug.LogWarning($"[FigureBattle] デッキに入れられるのは{MaxFigures}体までです");
                figures.RemoveRange(MaxFigures, figures.Count - MaxFigures);
            }
        }
    }
}
