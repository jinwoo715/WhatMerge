#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Skill.Data;
using System.Linq;

[CustomEditor(typeof(EffectValueEnhanceData))]
public class EffectStatEnhanceDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(serializedObject, "TargetStatKey", "AddValue");

        var data = (EffectValueEnhanceData)target;
        EffectBase targetEffect = data.TargetEffect;

        if (targetEffect is IEffectValueModifier modifier)
        {
            var stats = modifier.GetEnhanceableStats();

            string[] labels = stats.Select(x => x.Label).ToArray();
            int currentIndex = 0;

            for (int i = 0; i < stats.Count; i++)
            {
                if (stats[i].Key == data.TargetStatKey)
                {
                    currentIndex = i;
                    break;
                }
            }

            int selectedIndex = EditorGUILayout.Popup("Target Stat", currentIndex, labels);

            data.TargetStatKey = stats[selectedIndex].Key;
            data.AddValue = EditorGUILayout.FloatField("Add Value", data.AddValue);
        }
        else
        {
            EditorGUILayout.HelpBox("선택한 Effect는 강화 가능한 수치가 없습니다.", MessageType.Info);
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(data);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif