using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LoadingTips", menuName = "Arkvain/UI/Loading Tips")]
public class LoadingTipsSO : ScriptableObject
{
    [Header("General Gameplay Tips")]
    [TextArea(3, 10)]
    public List<string> tips = new List<string>();

    [Header("Lore & Flavor Text")]
    [TextArea(3, 10)]
    public List<string> flavorTexts = new List<string>();

    public string GetRandomAny()
    {
        int total = tips.Count + flavorTexts.Count;
        if (total == 0) return "Loading...";

        int index = Random.Range(0, total);
        if (index < tips.Count)
            return tips[index];
        else
            return flavorTexts[index - tips.Count];
    }

    public string GetRandomTip()
    {
        if (tips.Count == 0) return GetRandomAny();
        return tips[Random.Range(0, tips.Count)];
    }

    public string GetRandomFlavor()
    {
        if (flavorTexts.Count == 0) return GetRandomAny();
        return flavorTexts[Random.Range(0, flavorTexts.Count)];
    }
}
