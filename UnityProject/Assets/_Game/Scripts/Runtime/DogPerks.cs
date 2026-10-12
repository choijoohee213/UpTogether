using System.Collections.Generic;
using UnityEngine;

namespace UpTogether
{
    /// 강아지 특기. 친밀도 문턱을 넘을 때마다 하나 고른다.
    /// 문턱이 세 번(Bond 단계 20·40·62)이라 네 가지 중 셋만 가질 수 있다 — 고민이 생기도록.
    /// 고른 것은 PlayerPrefs 에 남아 다시 켜도 유지된다 (친밀도와 같은 수명).
    public static class DogPerks
    {
        public enum Kind { Super, Glutton, Clever, Cozy }

        public struct Info
        {
            public Kind kind;
            public string name;
            public string desc;
        }

        public static readonly Info[] All =
        {
            new Info { kind = Kind.Super,   name = "슈퍼맨 강아지",
                       desc = "크게 떨어질 때 물어 올려 구해준다" },
            new Info { kind = Kind.Glutton, name = "먹보 강아지",
                       desc = "손이 닿지 않는 간식을 가져온다" },
            new Info { kind = Kind.Clever,  name = "똑똑한 강아지",
                       desc = "위험한 것이 가까우면 짖어 알려준다" },
            new Info { kind = Kind.Cozy,    name = "포근한 강아지",
                       desc = "가시에 찔릴 때 한 번 막아준다" },
        };

        /// 이 친밀도를 넘을 때마다 하나 고른다. Bond 의 단계 문턱과 같이 둔다.
        public static readonly float[] Thresholds = { 20f, 40f, 62f };

        const string OwnedKey = "perk_owned";     // "0,2" 처럼 쉼표로 잇는다
        const string ClaimedKey = "perk_claimed"; // 몇 번째 문턱까지 골랐나

        public static Info Of(Kind k) => All[(int)k];

        public static List<Kind> Owned
        {
            get
            {
                var list = new List<Kind>();
                string s = PlayerPrefs.GetString(OwnedKey, "");
                if (string.IsNullOrEmpty(s)) return list;
                foreach (var part in s.Split(','))
                    if (int.TryParse(part, out int v) && v >= 0 && v < All.Length)
                        list.Add((Kind)v);
                return list;
            }
        }

        public static bool Has(Kind k) => Owned.Contains(k);

        public static void Take(Kind k)
        {
            var owned = Owned;
            if (owned.Contains(k)) return;
            owned.Add(k);
            var parts = new List<string>();
            foreach (var o in owned) parts.Add(((int)o).ToString());
            PlayerPrefs.SetString(OwnedKey, string.Join(",", parts));
            PlayerPrefs.SetInt(ClaimedKey, Claimed + 1);
            PlayerPrefs.Save();
        }

        /// 아직 안 고른 것들
        public static List<Kind> Remaining
        {
            get
            {
                var owned = Owned;
                var list = new List<Kind>();
                foreach (var i in All) if (!owned.Contains(i.kind)) list.Add(i.kind);
                return list;
            }
        }

        public static int Claimed => PlayerPrefs.GetInt(ClaimedKey, 0);

        /// 이 친밀도에서 고를 차례가 왔나
        public static bool PickDue(float bond)
        {
            int c = Claimed;
            return c < Thresholds.Length && bond >= Thresholds[c] && Remaining.Count > 0;
        }

        /// 처음부터 다시 (디버그·새 여정용)
        public static void Clear()
        {
            PlayerPrefs.DeleteKey(OwnedKey);
            PlayerPrefs.DeleteKey(ClaimedKey);
            PlayerPrefs.Save();
        }
    }
}
