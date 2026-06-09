using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;

public class WordChanger : EditorWindow
{
    private string _originWord;
    private string _replaceWord;
    private string _path;

    [MenuItem("Tools/Data/WordChanger")]
    public static void WordChange()
    {
        var window = GetWindow<WordChanger>("WordChange");
        window.minSize = new Vector2(200, 300);
    }
    private void OnEnable()
    {
        _path = "Assets/";
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Old Word");
        _originWord = EditorGUILayout.TextField(_originWord);

        EditorGUILayout.LabelField("\n");
        EditorGUILayout.LabelField("Replace Word");

        _replaceWord = EditorGUILayout.TextField(_replaceWord);

        EditorGUILayout.LabelField("\n");
        EditorGUILayout.LabelField("Directory");

        _path = EditorGUILayout.TextField(_path);

        if (GUILayout.Button("Replace"))
        {
            ReplaceWord();
        }
    }

    private void ReplaceWord()
    {
        if (!CheckValidFolder())
        {
            AlertInValidPath();
            return;
        }

        Replace();
    }

    //00.Resources\Sprites\Entity\Heros\ShieldSoldier
    private void Replace()
    {
        string[] files = Directory.GetFiles(_path);

        foreach (string filePath in files)
        {
            // 2. 원래 파일 정보 추출
            string directory = Path.GetDirectoryName(filePath);    // 폴더 경로
            string fileName = Path.GetFileNameWithoutExtension(filePath); // 파일 이름 (확장자 제외)
            string extension = Path.GetExtension(filePath);       // 확장자 (.txt 등)

            if (!fileName.Contains(_originWord))
                continue;

            // 3. 새로운 파일 이름 규칙 설정 (예: 이름_modified.확장자)
            string newFileName = fileName.Replace(_originWord, _replaceWord)+extension;
            string newFilePath = Path.Combine(directory, newFileName);

            try
            {
                // 4. 이름 변경 (실제로는 파일을 이동시키는 개념)
                AssetDatabase.MoveAsset(filePath, newFilePath);
                Debug.Log($"변경 완료: {Path.GetFileName(filePath)} -> {newFileName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"오류 발생 ({Path.GetFileName(filePath)}) : {ex.Message}");
            }
        }
        AssetDatabase.Refresh();
    }

    private bool CheckValidFolder()
    {
        return Directory.Exists(_path);
    }
    private void AlertInValidPath()
    {
        bool ok = UnityEditor.EditorUtility.DisplayDialog(
        "경로 없음",
        $"{_path}경로가 존재하지 않습니다.",
        "확인"
        );
    }
}
