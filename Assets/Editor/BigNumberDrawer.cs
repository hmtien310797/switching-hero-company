#if UNITY_EDITOR
using Immortal_Switch.Scripts.Core;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(BigNumber))]
public class BigNumberDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty mantissaProp = property.FindPropertyRelative("Mantissa");
        SerializedProperty tierProp = property.FindPropertyRelative("Tier");

        BigNumber current = new BigNumber(
            mantissaProp.doubleValue,
            tierProp.intValue
        );

        string currentText = current.ToInputString();

        EditorGUI.BeginProperty(position, label, property);

        EditorGUI.BeginChangeCheck();

        string input = EditorGUI.TextField(position, label, currentText);

        if (EditorGUI.EndChangeCheck())
        {
            if (BigNumber.TryParseInputString(input, out BigNumber parsed))
            {
                mantissaProp.doubleValue = parsed.Mantissa;
                tierProp.intValue = parsed.Tier;
            }
            else
            {
                Debug.LogError($"[BigNumberDrawer] Cannot parse BigNumber input: {input}");
            }
        }

        EditorGUI.EndProperty();
    }
}
#endif