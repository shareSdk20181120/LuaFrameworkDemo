using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LuaFramework;
using System;
using UObject = UnityEngine.Object;

public class TestLoadRes
{
    private static TestLoadRes instance;
    public static TestLoadRes Instance
    {
        get
        {
            if (instance == null)
                instance = new TestLoadRes();
            return instance;
        }
    }
    private string baseDownLoadURL;
    private string[] allManifest;
    private AssetBundleManifest abManifest;
    private Dictionary<string, string[]> abDependenciesDic = new Dictionary<string, string[]>();
    private Dictionary<string, AssetBundleInfo> loadedABDic = new Dictionary<string, AssetBundleInfo>();
    private Dictionary<string, List<LoadABRequest>> loadABReqDic = new Dictionary<string, List<LoadABRequest>>();
    public class LoadABRequest
    {
        public Type type;
        public string[] assetNames;
        public Action<UObject[]> atcion;
    }

 
    public void Init( Action ok)
    {
        baseDownLoadURL = Util.GetRelativePath();//资源包根目录所在url
        Util.LogError("down url: " + baseDownLoadURL);
        LoadRes<AssetBundleManifest>(AppConst.AssetDir, new string[] { "AssetBundleManifest" }, delegate (UObject[] objs) {
            abManifest = objs[0] as AssetBundleManifest;
            allManifest = abManifest.GetAllAssetBundles();
            if (ok != null)
                ok();
        });
    }
    public void LoadPrefab<T>(string abName,string[] resNames,Action<UObject[]> action= null) where T:UObject
    {
        LoadRes<T>(abName, resNames,action);
    }
    private void LoadRes<T>(string abName, string[] resNames,Action<UObject[]> action=null) where T : UnityEngine.Object
    {
        abName = GetABPath(abName);
        LoadABRequest quest = new LoadABRequest();
        quest.type = typeof(T);
        quest.assetNames = resNames;
        quest.atcion = action;
        List<LoadABRequest> questList = null;
        if(!loadABReqDic.TryGetValue(abName,out questList))
        {
            questList = new List<LoadABRequest>();
            questList.Add(quest);
            loadABReqDic.Add(abName, questList);
            ClienMain.Instance.StartCor(OnLoadAssetFromABInfo<T>(abName));
        }
        else
        {
            questList.Add(quest);
        }

    }

    /// <summary>
    /// 根据abName，加载资源信息
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="abName"></param>
    /// <returns></returns>

    private IEnumerator OnLoadAssetFromABInfo<T>(string abName) where T:UnityEngine.Object
    {
        //根据资源包名获取包信息（assetBundle，referenceCount）
        AssetBundleInfo bundleInfo = GetLoadedAB(abName);
        if (bundleInfo == null)
        {
            yield return ClienMain.Instance.StartCor(OnLoadABInfo(abName, typeof(T)));
            bundleInfo = GetLoadedAB(abName);
            if(bundleInfo==null)
            {
                loadABReqDic.Remove(abName);
                Debug.LogError("on load ab fail  : " + abName);
                yield break;                    
            }
        }
        List<LoadABRequest> list = null;
        if(!loadABReqDic.TryGetValue(abName,out list))
        {
            loadABReqDic.Remove(abName);
            yield break;
        }

        //遍历所有对某个资源包的请求，加载需求的资源
        foreach(var m in list)
        {
            string[] assetNames = m.assetNames;
            List<UObject> result = new List<UObject>();
            AssetBundle ab = bundleInfo.m_AssetBundle;
            foreach(var n in assetNames)
            {
                AssetBundleRequest request= ab.LoadAssetAsync(n,m.type);
                yield return request;
                result.Add(request.asset);
            }
            if (m.atcion != null) //将加载的资源发送给调用的函数  
            {
                m.atcion(result.ToArray());
                m.atcion = null;
            }

           

            bundleInfo.m_ReferencedCount++;
        }
        loadABReqDic.Remove(abName);

        
    }

    /// <summary>
    /// 加载资源包和其依赖的资源到集合中
    /// </summary>
    /// <param name="abName"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    private IEnumerator OnLoadABInfo(string abName,Type type)
    {
        string url = baseDownLoadURL + abName;
        WWW download = null;
        if (type == typeof(AssetBundleManifest))
            download = new WWW(url);
        else
        {
            string[] deps = abManifest.GetAllDependencies(abName);
            if(deps.Length>0)
            {
                //加载所依赖的资源
                abDependenciesDic.Add(abName, deps);
                foreach(var m in deps)
                {
                    AssetBundleInfo abInfo;
                    if(loadedABDic.TryGetValue(m,out abInfo))
                    {
                        abInfo.m_ReferencedCount++;
                    }
                    else if(!loadABReqDic.ContainsKey(m))
                    {
                        yield return ClienMain.Instance.StartCor(OnLoadABInfo(m, type));
                    }
                    
                }
            }
            download = WWW.LoadFromCacheOrDownload(url, abManifest.GetAssetBundleHash(abName), 0);
        }
        yield return download;
        if (download != null)
        {
            AssetBundle abObj = download.assetBundle;
            if (abObj)
                loadedABDic.Add(abName, new AssetBundleInfo(abObj));
        }
        else
        {
            Util.LogError("down is null: " + url);

        }
    }
    /// <summary>
    /// 获取已加载的资源包
    /// </summary>
    /// <param name="abName"></param>
    /// <returns></returns>
    private AssetBundleInfo GetLoadedAB(string abName)
    {
        AssetBundleInfo bundle = null;
        loadedABDic.TryGetValue(abName, out bundle);
        if (bundle == null)
            return null;
        string[] dependencies = null;
        if (!abDependenciesDic.TryGetValue(abName, out dependencies))//没有记录所依赖的资源，这个资源包自身就是所请求的
            return bundle;
        foreach(var m in dependencies)//确保所有依赖的资源都被加载
        {
            AssetBundleInfo dep = null;
            loadedABDic.TryGetValue(m, out dep);
            if (dep == null)
                return null;
        }
        return bundle;
            
    }
    /// <summary>
    /// 从allManifest中找到指定的ab路径
    /// </summary>
    /// <param name="abName"></param>
    /// <returns></returns>
    private string GetABPath(string abName)
    {
        if (abName.Equals(AppConst.AssetDir))//包名为assetBundles根目录名，这个文件包含所有manifest
            return abName;
        abName = abName.ToLower();
        if (!abName.EndsWith(AppConst.ExtName))
            abName += AppConst.ExtName;
        if (abName.Contains("/"))
            return abName;
        foreach(var m in allManifest)
        {
            Util.LogError("manifest:  " + m);//login.unity3d
            int index = m.LastIndexOf("/");
            string path = m.Remove(0, index + 1);
            if (path.Equals(abName))
                return m;
        }
        Util.LogError("get ab path fail: " + abName);
        return null;
    }
}
