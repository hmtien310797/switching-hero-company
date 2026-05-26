using System.IO;
using UnityEditor;
using UnityEngine;

public class SpriteCombinerWindow : EditorWindow
{
    [SerializeField] private Sprite[] sprites;

    [SerializeField] private int columns = 2;
    [SerializeField] private int rows = 2;

    [SerializeField] private string outputFileName = "CombinedSprite.png";

    [MenuItem("Tools/Sprite Combiner")]
    private static void Open()
    {
        GetWindow<SpriteCombinerWindow>("Sprite Combiner");
    }

    private void OnGUI()
    {
        SerializedObject so = new SerializedObject(this);

        EditorGUILayout.PropertyField(so.FindProperty(nameof(sprites)), true);

        columns = EditorGUILayout.IntField("Columns", columns);
        rows = EditorGUILayout.IntField("Rows", rows);
        outputFileName = EditorGUILayout.TextField("Output File Name", outputFileName);

        so.ApplyModifiedProperties();

        EditorGUILayout.Space();

        if (GUILayout.Button("Combine Sprites"))
        {
            Combine();
        }
    }

    private void Combine()
    {
        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogError("No sprites assigned.");
            return;
        }

        if (columns <= 0 || rows <= 0)
        {
            Debug.LogError("Columns and rows must be greater than 0.");
            return;
        }

        if (sprites.Length > columns * rows)
        {
            Debug.LogError("Sprite count is greater than columns * rows.");
            return;
        }

        Sprite firstSprite = sprites[0];

        int cellWidth = Mathf.RoundToInt(firstSprite.rect.width);
        int cellHeight = Mathf.RoundToInt(firstSprite.rect.height);

        int outputWidth = cellWidth * columns;
        int outputHeight = cellHeight * rows;

        Texture2D output = new Texture2D(outputWidth, outputHeight, TextureFormat.RGBA32, false);
        Color32[] clearPixels = new Color32[outputWidth * outputHeight];

        for (int i = 0; i < clearPixels.Length; i++)
            clearPixels[i] = new Color32(0, 0, 0, 0);

        output.SetPixels32(clearPixels);

        for (int i = 0; i < sprites.Length; i++)
        {
            Sprite sprite = sprites[i];

            if (sprite == null)
                continue;

            Texture2D sourceTexture = GetReadableTexture(sprite.texture);

            Rect rect = sprite.rect;

            int sourceX = Mathf.RoundToInt(rect.x);
            int sourceY = Mathf.RoundToInt(rect.y);
            int width = Mathf.RoundToInt(rect.width);
            int height = Mathf.RoundToInt(rect.height);

            if (width != cellWidth || height != cellHeight)
            {
                Debug.LogWarning($"{sprite.name} size is different from first sprite. It may not align correctly.");
            }

            Color[] pixels = sourceTexture.GetPixels(sourceX, sourceY, width, height);

            int column = i % columns;
            int row = i / columns;

            int destX = column * cellWidth;

            // Đảo row để sprite[0] nằm ở góc trên trái theo logic layout thông thường.
            int destY = outputHeight - ((row + 1) * cellHeight);

            output.SetPixels(destX, destY, width, height, pixels);
        }

        output.Apply();

        string folderPath = "Assets/CombinedSprites";

        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        string outputPath = Path.Combine(folderPath, outputFileName);

        if (!outputPath.EndsWith(".png"))
            outputPath += ".png";

        File.WriteAllBytes(outputPath, output.EncodeToPNG());

        AssetDatabase.Refresh();

        TextureImporter importer = AssetImporter.GetAtPath(outputPath) as TextureImporter;

        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        Debug.Log($"Combined sprite saved to: {outputPath}");
    }

    private Texture2D GetReadableTexture(Texture2D source)
    {
        RenderTexture rt = RenderTexture.GetTemporary(
            source.width,
            source.height,
            0,
            RenderTextureFormat.Default,
            RenderTextureReadWrite.Linear
        );

        Graphics.Blit(source, rt);

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D readableTexture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        readableTexture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        readableTexture.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        return readableTexture;
    }
}