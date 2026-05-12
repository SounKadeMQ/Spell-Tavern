using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

//here cause wouldnt properly apply to everything^^
public static class ApplyFinalFantasyFont
{
    const string SourceFontPath = "Assets/TextMesh Pro/Fonts/Final-Fantasy.ttf";
    const string FontAssetPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/Final-Fantasy SDF.asset";
    const string OldFontGuid = "8f586378b4e144a9851e7b34d9b748ee";
    const string OldFontReference = "m_fontAsset: {fileID: 11400000, guid: " + OldFontGuid + ", type: 2}";
    const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

    public static void Run()
    {
        AssetDatabase.ImportAsset(SourceFontPath, ImportAssetOptions.ForceUpdate);

        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (fontAsset == null)
        {
            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
            if (sourceFont == null)
            {
                throw new FileNotFoundException("Could not load source font.", SourceFontPath);
            }

            fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                90,
                9,
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic,
                true);

            fontAsset.name = "Final-Fantasy SDF";
            if (fontAsset.material != null)
            {
                fontAsset.material.name = "Final-Fantasy SDF Material";
            }

            AssetDatabase.CreateAsset(fontAsset, FontAssetPath);
            AssetDatabase.SaveAssets();
        }

        string newFontGuid = AssetDatabase.AssetPathToGUID(FontAssetPath);
        string newFontReference = "m_fontAsset: {fileID: 11400000, guid: " + newFontGuid + ", type: 2}";

        UpdateTmpSettings(newFontGuid);
        ReplaceFontReferences(newFontReference);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Applied Final-Fantasy TMP font asset to project text references.");
    }

    static void UpdateTmpSettings(string newFontGuid)
    {
        string text = File.ReadAllText(TmpSettingsPath);
        text = text.Replace(
            "m_defaultFontAsset: {fileID: 11400000, guid: " + OldFontGuid + ", type: 2}",
            "m_defaultFontAsset: {fileID: 11400000, guid: " + newFontGuid + ", type: 2}");
        File.WriteAllText(TmpSettingsPath, text);
    }

    static void ReplaceFontReferences(string newFontReference)
    {
        string[] files = Directory.GetFiles("Assets/Scenes/Gameplay", "*.*", SearchOption.AllDirectories);
        for (int i = 0; i < files.Length; i++)
        {
            string path = files[i].Replace('\\', '/');
            if (!path.EndsWith(".unity") && !path.EndsWith(".prefab"))
            {
                continue;
            }

            string text = File.ReadAllText(path);
            if (!text.Contains(OldFontReference))
            {
                continue;
            }

            File.WriteAllText(path, text.Replace(OldFontReference, newFontReference));
        }
    }
}

[InitializeOnLoad]
public static class ApplyFinalFantasyFontOnLoad
{
    static ApplyFinalFantasyFontOnLoad()
    {
        EditorApplication.delayCall += RunOnce;
    }

    static void RunOnce()
    {
        if (SessionState.GetBool("SpellTavern.FinalFantasyFont.RanThisSession", false))
        {
            return;
        }

        if (EditorPrefs.GetBool("SpellTavern.FinalFantasyFont.Applied", false))
        {
            return;
        }

        SessionState.SetBool("SpellTavern.FinalFantasyFont.RanThisSession", true);
        ApplyFinalFantasyFont.Run();
        EditorPrefs.SetBool("SpellTavern.FinalFantasyFont.Applied", true);
    }
}
