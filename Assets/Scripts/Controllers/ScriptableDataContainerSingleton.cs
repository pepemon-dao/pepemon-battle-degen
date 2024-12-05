using Pepemon.Battle;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScriptableDataContainerSingleton : MonoBehaviour
{
    [TitleGroup("Scriptable objects list"), SerializeField] public DataContainer CardsScriptableObjsData;

    public static ScriptableDataContainerSingleton Instance;

    private void Awake()
    {
        Instance = this;
    }
}
