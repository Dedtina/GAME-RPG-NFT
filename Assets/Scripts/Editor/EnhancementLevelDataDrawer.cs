#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(EnhancementLevelData))]
public class EnhancementLevelDataDrawer : PropertyDrawer
{
    private const float RequiredTotalChance = 100f;
    private const float ChanceTolerance = .001f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        Rect foldoutRect = GetLineRect(position, position.y);
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, BuildFoldoutLabel(property, label), true);

        if (!property.isExpanded)
        {
            EditorGUI.EndProperty();
            return;
        }

        EditorGUI.indentLevel++;
        float y = foldoutRect.yMax + EditorGUIUtility.standardVerticalSpacing;

        DrawProperty(position, ref y, property.FindPropertyRelative("level"));
        DrawProperty(position, ref y, property.FindPropertyRelative("statMultiplier"));
        DrawProperty(position, ref y, property.FindPropertyRelative("currencyCost"));
        DrawProperty(position, ref y, property.FindPropertyRelative("requiredMaterials"), true);

        y += EditorGUIUtility.standardVerticalSpacing;
        EditorGUI.LabelField(GetLineRect(position, y), "Outcome Chances", EditorStyles.boldLabel);
        y += GetLineHeight();

        DrawChanceSlider(position, ref y, property.FindPropertyRelative("successChance"), "Success");
        DrawChanceSlider(position, ref y, property.FindPropertyRelative("downgradeChance"), "Downgrade");
        DrawChanceSlider(position, ref y, property.FindPropertyRelative("resetChance"), "Reset");
        DrawChanceSlider(position, ref y, property.FindPropertyRelative("noChangeChance"), "No Change");
        DrawProperty(position, ref y, property.FindPropertyRelative("downgradeLevels"));

        DrawTotalChance(position, ref y, property);
        DrawNormalizeButton(position, ref y, property);

        EditorGUI.indentLevel--;
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = GetLineHeight();
        if (!property.isExpanded)
            return height;

        height += GetLineHeight() * 3;
        height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("requiredMaterials"), true) + EditorGUIUtility.standardVerticalSpacing;
        height += EditorGUIUtility.standardVerticalSpacing;
        height += GetLineHeight();
        height += GetLineHeight() * 5;
        height += GetLineHeight();
        height += GetLineHeight();

        if (!HasValidTotalChance(property))
            height += GetHelpBoxHeight();

        return height;
    }

    private static GUIContent BuildFoldoutLabel(SerializedProperty property, GUIContent fallbackLabel)
    {
        SerializedProperty level = property.FindPropertyRelative("level");
        return new GUIContent(level != null ? $"Level +{level.intValue}" : fallbackLabel.text);
    }

    private static void DrawProperty(Rect position, ref float y, SerializedProperty property, bool includeChildren = false)
    {
        float height = EditorGUI.GetPropertyHeight(property, includeChildren);
        Rect rect = new(position.x, y, position.width, height);
        EditorGUI.PropertyField(rect, property, includeChildren);
        y += height + EditorGUIUtility.standardVerticalSpacing;
    }

    private static void DrawChanceSlider(Rect position, ref float y, SerializedProperty property, string label)
    {
        Rect rect = GetLineRect(position, y);
        property.floatValue = EditorGUI.Slider(rect, label, property.floatValue, 0, 100);
        y += GetLineHeight();
    }

    private static void DrawTotalChance(Rect position, ref float y, SerializedProperty property)
    {
        float totalChance = GetTotalChance(property);
        bool isValid = IsValidTotal(totalChance);
        Rect rect = GetLineRect(position, y);

        Color originalColor = GUI.color;
        GUI.color = isValid ? Color.green : Color.yellow;
        EditorGUI.LabelField(rect, "Total Chance", $"{totalChance:0.##} / {RequiredTotalChance:0}");
        GUI.color = originalColor;

        y += GetLineHeight();

        if (!isValid)
        {
            Rect helpRect = new(position.x, y, position.width, GetHelpBoxHeight() - EditorGUIUtility.standardVerticalSpacing);
            EditorGUI.HelpBox(helpRect, "Total chance must be exactly 100 before this level can enhance.", MessageType.Error);
            y += GetHelpBoxHeight();
        }
    }

    private static void DrawNormalizeButton(Rect position, ref float y, SerializedProperty property)
    {
        Rect rect = GetLineRect(position, y);
        if (GUI.Button(rect, "Normalize Chances To 100"))
            NormalizeChances(property);

        y += GetLineHeight();
    }

    private static void NormalizeChances(SerializedProperty property)
    {
        SerializedProperty success = property.FindPropertyRelative("successChance");
        SerializedProperty downgrade = property.FindPropertyRelative("downgradeChance");
        SerializedProperty reset = property.FindPropertyRelative("resetChance");
        SerializedProperty noChange = property.FindPropertyRelative("noChangeChance");

        float total = GetTotalChance(property);
        if (total <= 0)
        {
            success.floatValue = RequiredTotalChance;
            downgrade.floatValue = 0;
            reset.floatValue = 0;
            noChange.floatValue = 0;
            return;
        }

        success.floatValue = RoundChance(success.floatValue / total * RequiredTotalChance);
        downgrade.floatValue = RoundChance(downgrade.floatValue / total * RequiredTotalChance);
        reset.floatValue = RoundChance(reset.floatValue / total * RequiredTotalChance);
        noChange.floatValue = RequiredTotalChance - success.floatValue - downgrade.floatValue - reset.floatValue;
    }

    private static float GetTotalChance(SerializedProperty property)
    {
        return property.FindPropertyRelative("successChance").floatValue
            + property.FindPropertyRelative("downgradeChance").floatValue
            + property.FindPropertyRelative("resetChance").floatValue
            + property.FindPropertyRelative("noChangeChance").floatValue;
    }

    private static bool HasValidTotalChance(SerializedProperty property)
    {
        return IsValidTotal(GetTotalChance(property));
    }

    private static bool IsValidTotal(float totalChance)
    {
        return Mathf.Abs(totalChance - RequiredTotalChance) <= ChanceTolerance;
    }

    private static float RoundChance(float value)
    {
        return Mathf.Round(value * 100f) / 100f;
    }

    private static Rect GetLineRect(Rect position, float y)
    {
        return new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
    }

    private static float GetLineHeight()
    {
        return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
    }

    private static float GetHelpBoxHeight()
    {
        return EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing;
    }
}
#endif
