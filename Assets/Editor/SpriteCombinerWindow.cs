using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class SpriteCombinerWindow : EditorWindow
{
    [Serializable]
    private class PlacedSprite
    {
        public Sprite sprite;
        public Vector2 position;
        public Vector2 size;
        public string name;
    }

    [SerializeField] private List<PlacedSprite> placedSprites = new();

    [SerializeField] private int canvasWidth = 2048;
    [SerializeField] private int canvasHeight = 512;

    [SerializeField] private float previewScale = 0.5f;
    [SerializeField] private bool snapToPixel = true;
    [SerializeField] private bool drawCheckerBackground = true;

    [SerializeField] private string outputFolder = "Assets/CombinedSprites";
    [SerializeField] private string outputFileName = "CombinedSprite.png";

    private Vector2 scroll;
    private int selectedIndex = -1;

    private bool isDraggingItem;
    private int draggingIndex = -1;
    private Vector2 dragMouseStartCanvas;
    private Vector2 dragItemStartPosition;

    private Rect canvasRect;

    [MenuItem("Tools/Sprite Combiner Visual")]
    private static void Open()
    {
        GetWindow<SpriteCombinerWindow>("Sprite Combiner");
    }

    private void OnGUI()
    {
        DrawTopSettings();

        EditorGUILayout.Space(8);

        DrawToolbar();

        EditorGUILayout.Space(8);

        DrawMainArea();

        HandleKeyboard();
    }

    private void DrawTopSettings()
    {
        EditorGUILayout.LabelField("Output Settings", EditorStyles.boldLabel);

        using (new EditorGUILayout.VerticalScope("box"))
        {
            canvasWidth = Mathf.Max(1, EditorGUILayout.IntField("Canvas Width", canvasWidth));
            canvasHeight = Mathf.Max(1, EditorGUILayout.IntField("Canvas Height", canvasHeight));

            previewScale = EditorGUILayout.Slider("Preview Scale", previewScale, 0.05f, 2f);
            snapToPixel = EditorGUILayout.Toggle("Snap To Pixel", snapToPixel);
            drawCheckerBackground = EditorGUILayout.Toggle("Checker Background", drawCheckerBackground);

            outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
            outputFileName = EditorGUILayout.TextField("Output File Name", outputFileName);
        }
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Clear All", GUILayout.Height(28)))
            {
                if (EditorUtility.DisplayDialog("Clear All", "Remove all placed sprites?", "Yes", "No"))
                {
                    placedSprites.Clear();
                    selectedIndex = -1;
                }
            }

            if (GUILayout.Button("Auto Size To Bounds", GUILayout.Height(28)))
            {
                AutoSizeToBounds();
            }

            if (GUILayout.Button("Combine", GUILayout.Height(28)))
            {
                Combine();
            }
        }

        EditorGUILayout.HelpBox(
            "Drag sprites from Project into the canvas. Click and drag placed sprites to move them. Select a sprite to edit position and size.",
            MessageType.Info
        );
    }

    private void DrawMainArea()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawCanvasArea();

            GUILayout.Space(8);

            DrawSelectedInspector();
        }
    }

    private void DrawCanvasArea()
    {
        float viewWidth = Mathf.Max(300, canvasWidth * previewScale + 30);
        float viewHeight = Mathf.Max(300, canvasHeight * previewScale + 30);

        using (var scrollScope = new EditorGUILayout.ScrollViewScope(scroll, GUILayout.Width(position.width * 0.68f)))
        {
            scroll = scrollScope.scrollPosition;

            Rect areaRect = GUILayoutUtility.GetRect(viewWidth, viewHeight);

            canvasRect = new Rect(
                areaRect.x + 15,
                areaRect.y + 15,
                canvasWidth * previewScale,
                canvasHeight * previewScale
            );

            DrawCanvasBackground(canvasRect);
            DrawPlacedSprites(canvasRect);
            HandleCanvasEvents(canvasRect);
        }
    }

    private void DrawCanvasBackground(Rect rect)
    {
        EditorGUI.DrawRect(rect, new Color(0.16f, 0.16f, 0.16f, 1f));

        if (drawCheckerBackground)
        {
            DrawChecker(rect, 16);
        }

        Handles.color = Color.white;
        Handles.DrawAAPolyLine(
            2f,
            new Vector3(rect.xMin, rect.yMin),
            new Vector3(rect.xMax, rect.yMin),
            new Vector3(rect.xMax, rect.yMax),
            new Vector3(rect.xMin, rect.yMax),
            new Vector3(rect.xMin, rect.yMin)
        );

        GUI.Label(
            new Rect(rect.x + 6, rect.y + 4, 300, 20),
            $"{canvasWidth} x {canvasHeight}",
            EditorStyles.whiteMiniLabel
        );
    }

    private void DrawChecker(Rect rect, int cellSize)
    {
        Color a = new Color(0.22f, 0.22f, 0.22f, 1f);
        Color b = new Color(0.28f, 0.28f, 0.28f, 1f);

        int xCount = Mathf.CeilToInt(rect.width / cellSize);
        int yCount = Mathf.CeilToInt(rect.height / cellSize);

        for (int y = 0; y < yCount; y++)
        {
            for (int x = 0; x < xCount; x++)
            {
                Rect cell = new Rect(
                    rect.x + x * cellSize,
                    rect.y + y * cellSize,
                    cellSize,
                    cellSize
                );

                EditorGUI.DrawRect(cell, (x + y) % 2 == 0 ? a : b);
            }
        }
    }

    private void DrawPlacedSprites(Rect canvas)
    {
        for (int i = 0; i < placedSprites.Count; i++)
        {
            PlacedSprite item = placedSprites[i];

            if (item == null || item.sprite == null)
                continue;

            Rect itemRect = PixelRectToGuiRect(item.position, item.size, canvas);

            Rect uv = GetSpriteUV(item.sprite);

            GUI.DrawTextureWithTexCoords(itemRect, item.sprite.texture, uv, true);

            if (i == selectedIndex)
            {
                Handles.color = Color.yellow;
                Handles.DrawAAPolyLine(
                    3f,
                    new Vector3(itemRect.xMin, itemRect.yMin),
                    new Vector3(itemRect.xMax, itemRect.yMin),
                    new Vector3(itemRect.xMax, itemRect.yMax),
                    new Vector3(itemRect.xMin, itemRect.yMax),
                    new Vector3(itemRect.xMin, itemRect.yMin)
                );
            }
        }
    }

    private void HandleCanvasEvents(Rect canvas)
    {
        Event e = Event.current;

        HandleSpriteDrop(e, canvas);

        if (!canvas.Contains(e.mousePosition))
            return;

        if (e.type == EventType.MouseDown && e.button == 0)
        {
            int hitIndex = GetSpriteIndexAtMouse(e.mousePosition, canvas);

            selectedIndex = hitIndex;

            if (hitIndex >= 0)
            {
                isDraggingItem = true;
                draggingIndex = hitIndex;
                dragMouseStartCanvas = GuiToPixelPosition(e.mousePosition, canvas);
                dragItemStartPosition = placedSprites[hitIndex].position;

                GUI.FocusControl(null);
                e.Use();
            }

            Repaint();
        }

        if (e.type == EventType.MouseDrag && isDraggingItem && draggingIndex >= 0)
        {
            Vector2 currentMouseCanvas = GuiToPixelPosition(e.mousePosition, canvas);
            Vector2 delta = currentMouseCanvas - dragMouseStartCanvas;

            Vector2 newPosition = dragItemStartPosition + delta;

            if (snapToPixel)
            {
                newPosition.x = Mathf.Round(newPosition.x);
                newPosition.y = Mathf.Round(newPosition.y);
            }

            placedSprites[draggingIndex].position = newPosition;

            e.Use();
            Repaint();
        }

        if (e.type == EventType.MouseUp)
        {
            isDraggingItem = false;
            draggingIndex = -1;
        }
    }

    private void HandleSpriteDrop(Event e, Rect canvas)
    {
        if (!canvas.Contains(e.mousePosition))
            return;

        if (e.type != EventType.DragUpdated && e.type != EventType.DragPerform)
            return;

        bool hasSprite = false;

        foreach (UnityEngine.Object obj in DragAndDrop.objectReferences)
        {
            if (obj is Sprite)
            {
                hasSprite = true;
                break;
            }

            if (obj is Texture2D texture)
            {
                string path = AssetDatabase.GetAssetPath(texture);
                UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);

                foreach (UnityEngine.Object asset in assets)
                {
                    if (asset is Sprite)
                    {
                        hasSprite = true;
                        break;
                    }
                }
            }
        }

        if (!hasSprite)
            return;

        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

        if (e.type == EventType.DragPerform)
        {
            DragAndDrop.AcceptDrag();

            Vector2 dropPixelPosition = GuiToPixelPosition(e.mousePosition, canvas);

            foreach (UnityEngine.Object obj in DragAndDrop.objectReferences)
            {
                AddDraggedObject(obj, dropPixelPosition);
            }

            e.Use();
            Repaint();
        }
        else
        {
            e.Use();
        }
    }

    private void AddDraggedObject(UnityEngine.Object obj, Vector2 dropPixelPosition)
    {
        if (obj is Sprite sprite)
        {
            AddSprite(sprite, dropPixelPosition);
            return;
        }

        if (obj is Texture2D texture)
        {
            string path = AssetDatabase.GetAssetPath(texture);
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);

            foreach (UnityEngine.Object asset in assets)
            {
                if (asset is Sprite childSprite)
                {
                    AddSprite(childSprite, dropPixelPosition);
                }
            }
        }
    }

    private void AddSprite(Sprite sprite, Vector2 dropPixelPosition)
    {
        Vector2 size = new Vector2(sprite.rect.width, sprite.rect.height);

        Vector2 position = dropPixelPosition;

        // Đặt tâm sprite vào vị trí thả chuột.
        position -= size * 0.5f;

        if (snapToPixel)
        {
            position.x = Mathf.Round(position.x);
            position.y = Mathf.Round(position.y);
        }

        PlacedSprite item = new PlacedSprite
        {
            sprite = sprite,
            position = position,
            size = size,
            name = sprite.name
        };

        placedSprites.Add(item);
        selectedIndex = placedSprites.Count - 1;
    }

    private void DrawSelectedInspector()
    {
        using (new EditorGUILayout.VerticalScope("box", GUILayout.Width(position.width * 0.28f)))
        {
            EditorGUILayout.LabelField("Placed Sprites", EditorStyles.boldLabel);

            DrawSpriteList();

            EditorGUILayout.Space(8);

            EditorGUILayout.LabelField("Selected", EditorStyles.boldLabel);

            if (selectedIndex < 0 || selectedIndex >= placedSprites.Count)
            {
                EditorGUILayout.HelpBox("No sprite selected.", MessageType.None);
                return;
            }

            PlacedSprite item = placedSprites[selectedIndex];

            EditorGUI.BeginChangeCheck();

            item.sprite = (Sprite)EditorGUILayout.ObjectField("Sprite", item.sprite, typeof(Sprite), false);
            item.name = EditorGUILayout.TextField("Name", item.name);

            item.position = EditorGUILayout.Vector2Field("Position", item.position);
            item.size = EditorGUILayout.Vector2Field("Size", item.size);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Use Original Size"))
                {
                    if (item.sprite != null)
                    {
                        item.size = new Vector2(item.sprite.rect.width, item.sprite.rect.height);
                    }
                }

                if (GUILayout.Button("Center"))
                {
                    item.position = new Vector2(
                        (canvasWidth - item.size.x) * 0.5f,
                        (canvasHeight - item.size.y) * 0.5f
                    );
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Move Up"))
                {
                    MoveSelectedLayer(1);
                }

                if (GUILayout.Button("Move Down"))
                {
                    MoveSelectedLayer(-1);
                }
            }

            if (GUILayout.Button("Delete Selected"))
            {
                placedSprites.RemoveAt(selectedIndex);
                selectedIndex = Mathf.Clamp(selectedIndex, -1, placedSprites.Count - 1);
                GUIUtility.ExitGUI();
            }

            if (EditorGUI.EndChangeCheck())
            {
                if (snapToPixel)
                {
                    item.position.x = Mathf.Round(item.position.x);
                    item.position.y = Mathf.Round(item.position.y);
                    item.size.x = Mathf.Round(item.size.x);
                    item.size.y = Mathf.Round(item.size.y);
                }

                Repaint();
            }

            EditorGUILayout.Space(8);

            EditorGUILayout.HelpBox(
                "Position uses top-left pixel coordinates. Later items in the list are rendered on top.",
                MessageType.Info
            );
        }
    }

    private void DrawSpriteList()
    {
        int removeIndex = -1;

        for (int i = 0; i < placedSprites.Count; i++)
        {
            PlacedSprite item = placedSprites[i];

            using (new EditorGUILayout.HorizontalScope())
            {
                bool selected = i == selectedIndex;

                string label = item != null && item.sprite != null
                    ? $"{i}. {item.sprite.name}"
                    : $"{i}. Missing Sprite";

                GUIStyle style = selected ? EditorStyles.boldLabel : EditorStyles.label;

                if (GUILayout.Button(label, style, GUILayout.Height(22)))
                {
                    selectedIndex = i;
                    GUI.FocusControl(null);
                    Repaint();
                }

                if (GUILayout.Button("X", GUILayout.Width(24)))
                {
                    removeIndex = i;
                }
            }
        }

        if (removeIndex >= 0)
        {
            placedSprites.RemoveAt(removeIndex);

            if (selectedIndex >= placedSprites.Count)
                selectedIndex = placedSprites.Count - 1;

            GUIUtility.ExitGUI();
        }
    }

    private int GetSpriteIndexAtMouse(Vector2 mouse, Rect canvas)
    {
        for (int i = placedSprites.Count - 1; i >= 0; i--)
        {
            PlacedSprite item = placedSprites[i];

            if (item == null || item.sprite == null)
                continue;

            Rect itemRect = PixelRectToGuiRect(item.position, item.size, canvas);

            if (itemRect.Contains(mouse))
                return i;
        }

        return -1;
    }

    private Rect PixelRectToGuiRect(Vector2 pixelPosition, Vector2 pixelSize, Rect canvas)
    {
        return new Rect(
            canvas.x + pixelPosition.x * previewScale,
            canvas.y + pixelPosition.y * previewScale,
            pixelSize.x * previewScale,
            pixelSize.y * previewScale
        );
    }

    private Vector2 GuiToPixelPosition(Vector2 guiPosition, Rect canvas)
    {
        return new Vector2(
            (guiPosition.x - canvas.x) / previewScale,
            (guiPosition.y - canvas.y) / previewScale
        );
    }

    private Rect GetSpriteUV(Sprite sprite)
    {
        Rect textureRect = sprite.rect;
        Texture2D texture = sprite.texture;

        return new Rect(
            textureRect.x / texture.width,
            textureRect.y / texture.height,
            textureRect.width / texture.width,
            textureRect.height / texture.height
        );
    }

    private void MoveSelectedLayer(int direction)
    {
        if (selectedIndex < 0 || selectedIndex >= placedSprites.Count)
            return;

        int newIndex = Mathf.Clamp(selectedIndex + direction, 0, placedSprites.Count - 1);

        if (newIndex == selectedIndex)
            return;

        PlacedSprite item = placedSprites[selectedIndex];
        placedSprites.RemoveAt(selectedIndex);
        placedSprites.Insert(newIndex, item);
        selectedIndex = newIndex;

        Repaint();
    }

    private void AutoSizeToBounds()
    {
        if (placedSprites.Count == 0)
            return;

        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;

        foreach (PlacedSprite item in placedSprites)
        {
            if (item == null || item.sprite == null)
                continue;

            minX = Mathf.Min(minX, item.position.x);
            minY = Mathf.Min(minY, item.position.y);
            maxX = Mathf.Max(maxX, item.position.x + item.size.x);
            maxY = Mathf.Max(maxY, item.position.y + item.size.y);
        }

        if (minX == float.MaxValue)
            return;

        foreach (PlacedSprite item in placedSprites)
        {
            if (item == null || item.sprite == null)
                continue;

            item.position -= new Vector2(minX, minY);
        }

        canvasWidth = Mathf.CeilToInt(maxX - minX);
        canvasHeight = Mathf.CeilToInt(maxY - minY);

        Repaint();
    }

    private void Combine()
    {
        if (canvasWidth <= 0 || canvasHeight <= 0)
        {
            Debug.LogError("Canvas size is invalid.");
            return;
        }

        if (placedSprites == null || placedSprites.Count == 0)
        {
            Debug.LogError("No sprites to combine.");
            return;
        }

        Texture2D output = new Texture2D(canvasWidth, canvasHeight, TextureFormat.RGBA32, false);

        Color32[] clearPixels = new Color32[canvasWidth * canvasHeight];

        for (int i = 0; i < clearPixels.Length; i++)
            clearPixels[i] = new Color32(0, 0, 0, 0);

        output.SetPixels32(clearPixels);

        foreach (PlacedSprite item in placedSprites)
        {
            if (item == null || item.sprite == null)
                continue;

            DrawSpriteToOutput(output, item);
        }

        output.Apply();

        if (!Directory.Exists(outputFolder))
            Directory.CreateDirectory(outputFolder);

        string outputPath = Path.Combine(outputFolder, outputFileName);

        if (!outputPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
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
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        Debug.Log($"Combined sprite saved to: {outputPath}");
    }

    private void DrawSpriteToOutput(Texture2D output, PlacedSprite item)
    {
        Sprite sprite = item.sprite;

        Texture2D sourceTexture = GetReadableTexture(sprite.texture);

        Rect sourceRect = sprite.rect;

        int sourceX = Mathf.RoundToInt(sourceRect.x);
        int sourceY = Mathf.RoundToInt(sourceRect.y);
        int sourceWidth = Mathf.RoundToInt(sourceRect.width);
        int sourceHeight = Mathf.RoundToInt(sourceRect.height);

        Color[] sourcePixels = sourceTexture.GetPixels(sourceX, sourceY, sourceWidth, sourceHeight);

        Texture2D sourceSpriteTexture = new Texture2D(sourceWidth, sourceHeight, TextureFormat.RGBA32, false);
        sourceSpriteTexture.SetPixels(sourcePixels);
        sourceSpriteTexture.Apply();

        int targetWidth = Mathf.Max(1, Mathf.RoundToInt(item.size.x));
        int targetHeight = Mathf.Max(1, Mathf.RoundToInt(item.size.y));

        Color[] finalPixels;

        if (targetWidth == sourceWidth && targetHeight == sourceHeight)
        {
            finalPixels = sourcePixels;
        }
        else
        {
            finalPixels = ScalePixels(sourceSpriteTexture, targetWidth, targetHeight);
        }

        int destX = Mathf.RoundToInt(item.position.x);

        // Tool dùng tọa độ top-left, Texture2D dùng bottom-left.
        int destY = canvasHeight - Mathf.RoundToInt(item.position.y) - targetHeight;

        BlitPixelsAlpha(output, finalPixels, targetWidth, targetHeight, destX, destY);
    }

    private void BlitPixelsAlpha(Texture2D output, Color[] sourcePixels, int width, int height, int destX, int destY)
    {
        for (int y = 0; y < height; y++)
        {
            int outputY = destY + y;

            if (outputY < 0 || outputY >= output.height)
                continue;

            for (int x = 0; x < width; x++)
            {
                int outputX = destX + x;

                if (outputX < 0 || outputX >= output.width)
                    continue;

                Color src = sourcePixels[y * width + x];

                if (src.a <= 0f)
                    continue;

                Color dst = output.GetPixel(outputX, outputY);

                Color blended = AlphaBlend(src, dst);

                output.SetPixel(outputX, outputY, blended);
            }
        }
    }

    private Color AlphaBlend(Color src, Color dst)
    {
        float outA = src.a + dst.a * (1f - src.a);

        if (outA <= 0f)
            return Color.clear;

        float outR = (src.r * src.a + dst.r * dst.a * (1f - src.a)) / outA;
        float outG = (src.g * src.a + dst.g * dst.a * (1f - src.a)) / outA;
        float outB = (src.b * src.a + dst.b * dst.a * (1f - src.a)) / outA;

        return new Color(outR, outG, outB, outA);
    }

    private Color[] ScalePixels(Texture2D source, int targetWidth, int targetHeight)
    {
        Color[] result = new Color[targetWidth * targetHeight];

        for (int y = 0; y < targetHeight; y++)
        {
            float v = targetHeight <= 1 ? 0f : y / (float)(targetHeight - 1);

            for (int x = 0; x < targetWidth; x++)
            {
                float u = targetWidth <= 1 ? 0f : x / (float)(targetWidth - 1);

                result[y * targetWidth + x] = source.GetPixelBilinear(u, v);
            }
        }

        return result;
    }

    private Texture2D GetReadableTexture(Texture2D source)
    {
        RenderTexture rt = RenderTexture.GetTemporary(
            source.width,
            source.height,
            0,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.Default
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

    private void HandleKeyboard()
    {
        Event e = Event.current;

        if (selectedIndex < 0 || selectedIndex >= placedSprites.Count)
            return;

        if (e.type != EventType.KeyDown)
            return;

        PlacedSprite item = placedSprites[selectedIndex];

        if (item == null)
            return;

        Vector2 delta = Vector2.zero;

        if (e.keyCode == KeyCode.LeftArrow)
            delta.x = -1;
        else if (e.keyCode == KeyCode.RightArrow)
            delta.x = 1;
        else if (e.keyCode == KeyCode.UpArrow)
            delta.y = -1;
        else if (e.keyCode == KeyCode.DownArrow)
            delta.y = 1;
        else if (e.keyCode == KeyCode.Delete || e.keyCode == KeyCode.Backspace)
        {
            placedSprites.RemoveAt(selectedIndex);
            selectedIndex = Mathf.Clamp(selectedIndex, -1, placedSprites.Count - 1);
            e.Use();
            Repaint();
            return;
        }

        if (delta != Vector2.zero)
        {
            if (e.shift)
                delta *= 10f;

            item.position += delta;

            e.Use();
            Repaint();
        }
    }
}