using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

public enum SerializeType
{
    Invalid,
    Array,
    List,
    Int,
    Float,
    Bool,
    String,
    Enum,
    Rect,
    Color,
    Vector3,
    LayerMask,
    AnimCurve,
    Bounds,
    UObject
}

public enum UITriggerType
{
    OnClick,
    OnMouseOver,
    OnMouseOut,
    OnPress,
    OnRelease,
    OnDoubleClick,
    OnDrag,
    OnDrop,
    OnInput,
    OnKey,
    OnSelect,
    OnScroll
}

//=========================================================================
[Serializable]
public class GenericValue
{
    #region Public Property
    //---------------------------------------------------------------------
    public int intValue;
    public float floatValue;
    public bool boolValue;
    public string stringValue;
    public Rect rectValue;
    public Color colorValue;
    public Vector3 vector3Value;
    public Bounds boundsValue;
    public AnimationCurve animCurveValue;
    public UnityEngine.Object objectValue;

    //---------------------------------------------------------------------
    public object value
    {
        get { return GetValue(); }
        set { SetValue(value); }
    }

    //---------------------------------------------------------------------
    public SerializeType type
    {
        get { return m_Type; }
    }
    #endregion

    #region Public Method
    //---------------------------------------------------------------------
    public GenericValue(GenericValue src)
    {
        this.value = src.value;
    }

    //---------------------------------------------------------------------
    public GenericValue(SerializeType type, object val)
    {
        m_Type = type;
        if (val != null)
        {
            value = val;
        }
    }

    //---------------------------------------------------------------------
    public static string GetFieldName(Type type)
    {
        if (ms_FieldInfos == null)
        {
            ms_FieldInfos = typeof(GenericValue).GetFields(
                BindingFlags.Public | BindingFlags.Instance);
        }

        for (int i = 0; i < ms_FieldInfos.Length; ++i)
        {
            FieldInfo fieldInfo = ms_FieldInfos[i];
            if (type == fieldInfo.FieldType ||
                type.IsSubclassOf(fieldInfo.FieldType))
            {
                return fieldInfo.Name;
            }
        }

        return null;
    }

    //---------------------------------------------------------------------
    public static SerializeType GetValueType(Type type)
    {
        if (type == typeof(int))
        {
            return SerializeType.Int;
        }
        else if (type == typeof(float))
        {
            return SerializeType.Float;
        }
        else if (type == typeof(bool))
        {
            return SerializeType.Bool;
        }
        else if (type == typeof(string))
        {
            return SerializeType.String;
        }
        else if (type == typeof(Color))
        {
            return SerializeType.Color;
        }
        else if (type == typeof(Vector3))
        {
            return SerializeType.Vector3;
        }
        else if (type == typeof(Rect))
        {
            return SerializeType.Rect;
        }
        else if (type == typeof(AnimationCurve))
        {
            return SerializeType.AnimCurve;
        }
        else if (type == typeof(Bounds))
        {
            return SerializeType.Bounds;
        }
        else if (type.IsSubclassOf(typeof(UnityEngine.Object)))
        {
            return SerializeType.UObject;
        }

        return SerializeType.Invalid;
    }
    #endregion

    #region Internal Method
    //---------------------------------------------------------------------
    private void SetValue(object val)
    {
        switch (m_Type)
        {
            case SerializeType.Int:
                intValue = (int)val;
                break;
            case SerializeType.Float:
                floatValue = (float)val;
                break;
            case SerializeType.Bool:
                boolValue = (bool)val;
                break;
            case SerializeType.String:
                stringValue = (string)val;
                break;
            case SerializeType.Color:
                colorValue = (Color)val;
                break;
            case SerializeType.Vector3:
                vector3Value = (Vector3)val;
                break;
            case SerializeType.Rect:
                rectValue = (Rect)val;
                break;
            case SerializeType.AnimCurve:
                animCurveValue = (AnimationCurve)val;
                break;
            case SerializeType.Bounds:
                boundsValue = (Bounds)val;
                break;
            case SerializeType.UObject:
                objectValue = (UnityEngine.Object)val;
                break;
        }
    }

    //---------------------------------------------------------------------
    private object GetValue()
    {
        switch (m_Type)
        {
            case SerializeType.Int:
                return intValue;
            case SerializeType.Float:
                return floatValue;
            case SerializeType.Bool:
                return boolValue;
            case SerializeType.String:
                return stringValue;
            case SerializeType.Color:
                return colorValue;
            case SerializeType.Vector3:
                return vector3Value;
            case SerializeType.Rect:
                return rectValue;
            case SerializeType.AnimCurve:
                return animCurveValue;
            case SerializeType.Bounds:
                return boundsValue;
            case SerializeType.UObject:
                // Unity object is null object, but is not equal null.
                if (objectValue == null)
                {
                    return null;
                }
                return objectValue;
        }

        return null;
    }
    #endregion

    #region Internal Member
    //---------------------------------------------------------------------
    [SerializeField]
    protected SerializeType m_Type;

    //---------------------------------------------------------------------
    private static FieldInfo[] ms_FieldInfos = null;
    #endregion
}

public class UIEventDispatcher : MonoBehaviour
{
    #region Public Property
    //---------------------------------------------------------------------
    [SerializeField]
    public Transform receiver = null;

    //---------------------------------------------------------------------
    public UITriggerType trigger = UITriggerType.OnClick;

    //---------------------------------------------------------------------
    [SerializeField, HideInInspector]
    public string methodName = string.Empty;

    //---------------------------------------------------------------------
    [SerializeField, HideInInspector]
    public GenericValue[] paramArray = null;
    #endregion

    #region Event Method
    //---------------------------------------------------------------------
    public event Function<UIEventDispatcher, UITriggerType, bool> eventGuard;
    #endregion

    #region Unity Method
    //---------------------------------------------------------------------
    protected void Awake()
    {
        PrepareEventData();
    }
    #endregion

    #region Internal Method
    //---------------------------------------------------------------------
    protected void PrepareEventData()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (receiver == null)
        {
            DebugHelper.LogWarning(
                "Event receiver is null.", gameObject);
            return;
        }

        if (string.IsNullOrEmpty(methodName))
        {
            DebugHelper.LogWarning(
                "Event method name is null.", gameObject);
            return;
        }

        string[] methodNameArray = methodName.Split('.');
        if (methodNameArray.Length != 2)
        {
            DebugHelper.LogError(
                "Event method name parse error: " +
                methodName, gameObject);
            return;
        }

        string componentName = methodNameArray[0];
        string realMethodName = methodNameArray[1];

        Component[] components = receiver.GetComponents<Component>();
        Type currentReceiverType = null;
        for (int i = 0; i < components.Length; ++i)
        {
            Component component = components[i];
            Type receiverType = component.GetType();
            //DataProxy dataProxy = component as DataProxy;
            //if (dataProxy != null)
            //{
            //    receiverType = dataProxy.type;
            //}

            if (receiverType.Name != componentName)
            {
                continue;
            }

            currentReceiverType = receiverType;
            m_Receiver = component;
            //if (dataProxy != null)
            //{
            //    m_Receiver = dataProxy.target as Component;
            //}
            break;
        }

        if (currentReceiverType == null)
        {
            DebugHelper.LogError(
                "Can not found component with event method name: " +
                methodName, gameObject);
            return;
        }

        m_MethodInfo = currentReceiverType.GetMethod(
            realMethodName,
            BindingFlags.NonPublic |
            BindingFlags.Instance |
            BindingFlags.Public);

        if (m_MethodInfo == null)
        {
            DebugHelper.LogError("Can not find out '" +
                methodName + "' method from '" +
                currentReceiverType.FullName + "' class.");
            return;
        }
    }

    //---------------------------------------------------------------------
    protected void OnClick()
    {
        TouchEvent(UITriggerType.OnClick);
    }

    //---------------------------------------------------------------------
    protected void OnHover(bool isOver)
    {
        if (isOver)
        {
            TouchEvent(UITriggerType.OnMouseOver);
        }
        else
        {
            TouchEvent(UITriggerType.OnMouseOut);
        }
    }

    //---------------------------------------------------------------------
    protected void OnPress(bool isPressed)
    {
        if (isPressed)
        {
            TouchEvent(UITriggerType.OnPress);
        }
        else
        {
            TouchEvent(UITriggerType.OnRelease);
        }
    }

    //---------------------------------------------------------------------
    protected void OnDoubleClick()
    {
        TouchEvent(UITriggerType.OnDoubleClick);
    }

    //---------------------------------------------------------------------
    protected void OnDrag(Vector2 delta)
    {
        TouchEvent<Vector2>(UITriggerType.OnDrag, delta);
    }

    //---------------------------------------------------------------------
    protected void OnDrop(GameObject go)
    {
        TouchEvent<GameObject>(UITriggerType.OnDrop, go);
    }

    //---------------------------------------------------------------------
    protected void OnInput(string text)
    {
        TouchEvent<string>(UITriggerType.OnInput, text);
    }

    //---------------------------------------------------------------------
    protected void OnKey(KeyCode key)
    {
        TouchEvent<KeyCode>(UITriggerType.OnKey, key);
    }

    //---------------------------------------------------------------------
    protected void OnSelect(bool selected)
    {
        TouchEvent<bool>(UITriggerType.OnSelect, selected);
    }

    //---------------------------------------------------------------------
    protected void OnScroll(float delta)
    {
        TouchEvent<float>(UITriggerType.OnScroll, delta);
    }

    //---------------------------------------------------------------------
    protected void TouchEvent(UITriggerType curTrigger)
    {
        if (receiver == null || trigger != curTrigger || m_MethodInfo == null)
        {
            return;
        }

        if (eventGuard != null && !eventGuard(this, curTrigger))
        {
            return;
        }

        object[] parameters = new object[paramArray.Length + 1];
        parameters[0] = this;
        for (int i = 0; i < paramArray.Length; ++i)
        {
            parameters[i + 1] = paramArray[i].value;
        }

        m_MethodInfo.Invoke(m_Receiver, parameters);
    }

    //---------------------------------------------------------------------
    protected void TouchEvent<T>(UITriggerType curTrigger, T evtData)
    {
        if (receiver == null || trigger != curTrigger || m_MethodInfo == null)
        {
            return;
        }

        if (eventGuard != null && !eventGuard(this, curTrigger))
        {
            return;
        }

        object[] parameters = new object[paramArray.Length + 2];
        parameters[0] = this;
        parameters[1] = evtData;
        for (int i = 0; i < paramArray.Length; ++i)
        {
            parameters[i + 2] = paramArray[i].value;
        }

        m_MethodInfo.Invoke(m_Receiver, parameters);
    }

    //---------------------------------------------------------------------
    protected static bool IsSubclassOf(Type src, Type target)
    {
        if (src == target)
        {
            return true;
        }

        if (src.IsSubclassOf(target))
        {
            return true;
        }

        return false;
    }

    #endregion

    #region Internal Member
    //---------------------------------------------------------------------
    protected MethodInfo m_MethodInfo = null;

    //---------------------------------------------------------------------
    protected Component m_Receiver = null;
    #endregion
}