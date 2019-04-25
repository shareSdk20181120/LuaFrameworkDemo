using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClienMain : MonoBehaviour
{
    private static ClienMain instance;
    public static ClienMain Instance
    {
        get
        {
            return instance;
        }
    }

    private void Awake()
    {
        instance = this;
    }
    // Use this for initialization
    void Start () {

        TestLoadRes.Instance.Init(LoadLoginPanel);
        
	}

    private void LoadLoginPanel()
    {
        TestLoadRes.Instance.LoadPrefab<GameObject>("login", new string[] { "LoginPanel" }, delegate (UnityEngine.Object[] objs)
        {
            Debug.LogError("load ok");
            if (objs.Length > 0)
            {
                GameObject go = objs[0] as GameObject;
                GameObject.Instantiate(go);
            }
        });
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public  Coroutine StartCor(IEnumerator ie)
    {
        Coroutine cor= StartCoroutine(ie);
        return cor;
    }
}
