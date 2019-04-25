using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using LuaFramework;
using System.IO;

public class PackResSettings
{
    static string ResABPath
    {
        get { return "TestStreamingAsset/"; }
    }

    static string DataPath
    {
        get { return Application.dataPath+"/"; }
    }

    static List<string> filePaths = new List<string>();
    static List<AssetBundleBuild> abBuild = new List<AssetBundleBuild>(); 

    [MenuItem("Test/GetPath")]
    public static void SetPackRes()
    {
        
        string hotResPath = Application.dataPath + AppConst.hotResPath;
        RecursiveHotRes(hotResPath);
        BuildAssetBundle();
    }
    [MenuItem("Test/Build Asset Bundles")]
    static void BuildABs()
    {
        // Put the bundles in a folder called "ABs" within the Assets folder.
        BuildPipeline.BuildAssetBundles("Assets/ABs", BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);
    }
    [MenuItem("Test/Single package")]
    public static void TestBuild()
    {
        string m= "Assets/LuaFramework/Examples/Builds/Prompt";
        string outPath = "Assets/" + ResABPath;
        string[] strs = Directory.GetFiles(m);
        if (strs.Length == 0)
            return;
        for(int i=0;i<strs.Length;i++)
        {
            strs[i] = strs[i].Replace("\\", "/");
        }
        AssetBundleBuild ab = new AssetBundleBuild();
        string name = m.Substring(m.LastIndexOf("/") + 1).ToLower();
        Util.LogError("m--  " + name);

        ab.assetBundleName = name;
        ab.assetNames = strs;
        BuildPipeline.BuildAssetBundles(outPath,new AssetBundleBuild[] { ab},BuildAssetBundleOptions.None,BuildTarget.StandaloneWindows);
        AssetDatabase.Refresh();
    }
    private static void BuildAssetBundle()
    {
        abBuild.Clear();
        foreach(var m in filePaths)//路径==文件所在文件夹路径
        {
            //Util.LogError("m " + m); //Assets/LuaFramework/Examples/Builds/Login
            string[] strs = Directory.GetFiles(m);
            if (strs.Length == 0)
                continue;
            for(int i=0;i<strs.Length;i++)
            {
                strs[i] = strs[i].Replace("\\", "/"); //Util.LogError("str " + strs[i]);//Assets/LuaFramework/Examples/Builds/Login/LoginPanel.prefab
            }
            AssetBundleBuild ab = new AssetBundleBuild();
            string name = m.Substring(m.LastIndexOf("/")+1).ToLower();//文件夹名
            ab.assetBundleName = name+AppConst.ExtName;
            ab.assetNames = strs;
            abBuild.Add(ab);
        }
       
        string tempResPath = DataPath + ResABPath;
        if (Directory.Exists(tempResPath))
            Directory.Delete(tempResPath,true);
        Directory.CreateDirectory(tempResPath);
        string outPath = "Assets/" + ResABPath;
        AssetBundleBuild[] buildArray = abBuild.ToArray();
        BuildPipeline.BuildAssetBundles(outPath, buildArray, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);
        BuildFilesIndex();
        AssetDatabase.Refresh();
    }

    private static void BuildFilesIndex()
    {
        string resPath = DataPath + ResABPath;
        RecursiveFilePath(resPath);
        string filesTxt = resPath + "files.txt";
        if (File.Exists(filesTxt))
            File.Delete(filesTxt);
        FileStream fs = new FileStream(filesTxt, FileMode.CreateNew);
        StreamWriter sw = new StreamWriter(fs);
        foreach(string path in filePaths)
        {
            if (path.EndsWith(".meta"))
                continue;
            string md5 = Util.md5file(path);
            sw.WriteLine(path.Replace(resPath, string.Empty) + "|" + md5);
        }
        sw.Close();
        fs.Close();
            


    }

    private static void RecursiveFilePath(string path)
    {
        filePaths.Clear();
        string[] files = Directory.GetFiles(path);
        string[] dirs = Directory.GetDirectories(path);
        foreach(var m in files)
        {
            if (m.EndsWith(".meta"))
                continue;
            filePaths.Add(m.Replace("\\", "/"));
        }
        foreach(var m in dirs)
        {
            RecursiveFilePath(m);
        }
    }

    /// <summary>
    /// 变量指定路径下 所有文件  包括子目录
    /// </summary>
    /// <param name="path"></param>
    private static void RecursiveHotRes(string path)
    {
        string[] files = Directory.GetFiles(path);
        string[] dirs = Directory.GetDirectories(path);
        foreach(var m in files)
        {
            if (m.EndsWith(".meta"))
                continue;
           
            string tempStr = m.Substring(m.IndexOf("Assets")).Replace("\\", "/");            //Util.LogError("RecursiveHotRes :" + tempStr);//D:/Demo/Lua/LuaFrameworkDemo/Assets/LuaFramework/Examples/Builds/Login/LoginPanel.prefab            
            tempStr = tempStr.Substring(0, tempStr.LastIndexOf("/"));//D:/Demo/Lua/LuaFrameworkDemo/Assets/LuaFramework/Examples/Builds/Login
            if (!filePaths.Contains(tempStr))                
                filePaths.Add(tempStr);//存放文件所在文件夹路径

        }

        foreach(var m in dirs)
        { 
            RecursiveHotRes(m);
        }
    }
}
