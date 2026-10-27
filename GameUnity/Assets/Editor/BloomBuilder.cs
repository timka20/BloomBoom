using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static partial class BloomBuilder
{
    public static void Inspect()
    {
        AssetDatabase.Refresh();
        var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Macanic.fbx");
        if(!model) throw new Exception("FBX not imported");
        var root=UnityEngine.Object.Instantiate(model);
        var lines=root.GetComponentsInChildren<Renderer>().Select(r=>$"{r.name} center={r.bounds.center:F3} size={r.bounds.size:F3} mats={string.Join(",",r.sharedMaterials.Select(m=>m?m.name:"NULL"))}");
        File.WriteAllLines("Logs/unity-model-inspection.txt",lines);
        UnityEngine.Object.DestroyImmediate(root);
    }
}
