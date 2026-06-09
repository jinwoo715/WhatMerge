using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.AddressableAssets;
using System;
using System.Threading.Tasks;

public interface IResourcesLoader
{
    void LoadResources();
}
public interface IResourcesReader
{
    SpriteAtlas GetAtlas(string name);
    TextAsset GetTextAsset(string name);
    T GetObject<T>(string name) where T : MonoBehaviour;
}

public class AddressableResourcesManager : MonoBehaviour, IResourcesLoader, IResourcesReader
{
    [SerializeField] private List<SpriteAtlas> _atlasList;
    [SerializeField] private List<TextAsset> _textAssetList;
    [SerializeField] private List<GameObject> _objList;

    private Dictionary<string, SpriteAtlas> _atlas = new Dictionary<string, SpriteAtlas>();
    private Dictionary<string, TextAsset> _texts = new Dictionary<string, TextAsset>();
    private Dictionary<string, GameObject> _objects = new Dictionary<string, GameObject>();

    public void LoadResources()
    {
        foreach (var atals in _atlasList)
        {
            _atlas.Add(atals.name, atals);
        }
        foreach (var text in _textAssetList)
        {
            _texts.Add(text.name, text);
        }
        foreach (var obj in _objList)
        {
            _objects.Add(obj.name, obj);
        }
    }

    public SpriteAtlas GetAtlas(string name)
    {
        if (_atlas.TryGetValue(name, out SpriteAtlas atals))
        {
            return atals;
        }
        else
        {
            Debug.LogError($"Not Exist Atlas : {name}");
            return null;
        }
    }

    public TextAsset GetTextAsset(string name)
    {
        if (_texts.TryGetValue(name, out TextAsset text))
        {
            return text;
        }
        else
        {
            Debug.LogError($"Not Exist Text : {name}");
            return null;
        }
    }
    public T GetObject<T>(string name) where T : MonoBehaviour
    {
        if (_objects.TryGetValue(name, out GameObject obj))
        {
            T ob = obj.GetComponent<T>();
            return ob;
        }
        else
        {
            Debug.LogError($"Not Exist Text : {name}");
            return null;
        }
    }
}
