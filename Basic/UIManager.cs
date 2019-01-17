using System;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

public class UIManager : MonoBehaviour
{
    #region Public Variable
    //---------------------------------------------------------------------
    public Transform windowRoot = null;

    //---------------------------------------------------------------------
    public Transform modelViewRoot = null;

    //---------------------------------------------------------------------
    public string windowFolder = string.Empty;
    #endregion

    #region Public Property
    //---------------------------------------------------------------------
    public static UIManager Instance
    {
        get
        {
            return ms_Instance;
        }
    }

    //---------------------------------------------------------------------
    public static int layer
    {
        get
        {
            if (Instance == null)
            {
                return -1;
            }

            return Instance.gameObject.layer;
        }
    }

    //---------------------------------------------------------------------
    public static GameObject sceneObject
    {
        get { return ms_SceneObject; }
    }
    #endregion

    #region Public Event
    //---------------------------------------------------------------------
    public static event Param1Handler<UIWindow> WindowLoadedEvent;

    //---------------------------------------------------------------------
    public static event Param1Handler<UIWindow> WindowPreparedEvent;

    //---------------------------------------------------------------------
    public static event Param1Handler<UIWindow> WindowInitializedEvent;

    //---------------------------------------------------------------------
    public static event Param1Handler<UIWindow> WindowRefreshEvent;

    //---------------------------------------------------------------------
    public static event Param1Handler<UIWindow> WindowCommandEvent;

    //---------------------------------------------------------------------
    public static event Param1Handler<UIWindow> WindowPreShowEvent;

    //---------------------------------------------------------------------
    public static event Param1Handler<UIWindow> WindowPostShowEvent;

    //---------------------------------------------------------------------
    public static event Param1Handler<UIWindow> WindowPreHideEvent;

    //---------------------------------------------------------------------
    public static event Param1Handler<UIWindow> WindowPostHideEvent;

    //---------------------------------------------------------------------
    public static event Param1Handler<UIWindow> WindowShutdownEvent;
    #endregion

    #region Event Method
    //---------------------------------------------------------------------
    internal static void TouchWindowLoaded(UIWindow win)
    {
        if (Instance != null)
        {
            Instance.AddWindow(win);
        }

        if (WindowLoadedEvent != null)
        {
            WindowLoadedEvent(win);
        }
    }

    //---------------------------------------------------------------------
    internal static void TouchWindowPreparedEvent(UIWindow win)
    {
        if (WindowPreparedEvent != null)
        {
            WindowPreparedEvent(win);
        }
    }

    //---------------------------------------------------------------------
    internal static void TouchWindowInitializedEvent(UIWindow win)
    {
        if (WindowInitializedEvent != null)
        {
            WindowInitializedEvent(win);
        }

        List<PendingInfo> pendingInfoList = null;
        if (!Instance.m_PendingWindows.TryGetValue(
            win.GetType(), out pendingInfoList))
        {
            return;
        }

        for (int index = 0; index < pendingInfoList.Count; ++index)
        {
            PendingInfo pendingInfo = pendingInfoList[index];
            if (pendingInfo.handler != null)
            {
                pendingInfo.handler(win, pendingInfo.data);
            }
        }

        Instance.m_PendingWindows.Remove(win.GetType());
    }

    //---------------------------------------------------------------------
    internal static void TouchWindowRefreshEvent(UIWindow win)
    {
        if (WindowRefreshEvent != null)
        {
            WindowRefreshEvent(win);
        }
    }

    //---------------------------------------------------------------------
    internal static void TouchWindowCommandEvent(UIWindow win)
    {
        if (WindowCommandEvent != null)
        {
            WindowCommandEvent(win);
        }
    }

    //---------------------------------------------------------------------
    internal static void TouchWindowPreShowEvent(UIWindow win)
    {
        if (WindowPreShowEvent != null)
        {
            WindowPreShowEvent(win);
        }
    }

    //---------------------------------------------------------------------
    internal static void TouchWindowPostShowEvent(UIWindow win)
    {
        if (WindowPostShowEvent != null)
        {
            WindowPostShowEvent(win);
        }
    }

    //---------------------------------------------------------------------
    internal static void TouchWindowPreHideEvent(UIWindow win)
    {
        if (WindowPreHideEvent != null)
        {
            WindowPreHideEvent(win);
        }

        if (Instance != null)
        {
            Instance.ExecutePopRollback(win);
        }
    }

    //---------------------------------------------------------------------
    internal static void TouchWindowPostHideEvent(UIWindow win)
    {
        if (WindowPostHideEvent != null)
        {
            WindowPostHideEvent(win);
        }
    }

    //---------------------------------------------------------------------
    internal static void TouchWindowShutdownEvent(UIWindow win)
    {
        if (!Instance.m_ShutdownWindows.Contains(win.GetType()))
        {
            Instance.m_ShutdownWindows.Add(win.GetType());
        }
    }

    //---------------------------------------------------------------------
    internal static void TouchWindowRequestShow(UIWindow win)
    {
        if (Instance == null)
        {
            return;
        }

        Instance.RequestPushRollback(win);
        if (Instance.m_ShutdownWindows.Contains(win.GetType()))
        {
            Instance.m_ShutdownWindows.Remove(win.GetType());
        }
    }

    //---------------------------------------------------------------------
    internal static void TouchWindowRequestHide(UIWindow win)
    {
        if (Instance == null)
        {
            return;
        }

        Instance.RequestPopRollback(win);
    }
    #endregion

    #region Public Method
    //---------------------------------------------------------------------
    public static void Wait(string group, float threshold, float timeout, string note,
        string message, int buttonMask, DefaultHandler retry, DefaultHandler ok)
    {
        Show(ms_WaittingType, delegate (UIWindow win, object data)
        {
            UIWaitingBase waitting = win as UIWaitingBase;
            if (waitting == null)
            {
                return;
            }

            waitting.Wait(group, threshold, timeout,
                note, message, buttonMask, retry, ok);
        });
    }

    //---------------------------------------------------------------------
    public static void Dialog(string group, string message, int buttonMask,
        DefaultHandler retryHandler, DefaultHandler okHandler)
    {
        Show(ms_WaittingType, delegate (UIWindow win, object data)
        {
            UIWaitingBase waitting = win as UIWaitingBase;
            if (waitting == null)
            {
                return;
            }

            waitting.Dialog(group, message, buttonMask, retryHandler, okHandler);
        });
    }

    //---------------------------------------------------------------------
    public static void StopWait(string group)
    {
        Window(ms_WaittingType, delegate (UIWindow win, object data)
        {
            UIWaitingBase waitting = win as UIWaitingBase;
            if (waitting == null)
            {
                return;
            }

            waitting.Stop(group);
        }, null);
    }

    //---------------------------------------------------------------------
    public static void StopWaitAll()
    {
        Window(ms_WaittingType, delegate (UIWindow win, object data)
        {
            UIWaitingBase waitting = win as UIWaitingBase;
            if (waitting == null)
            {
                return;
            }

            waitting.StopAll();
        }, null);
    }

    //---------------------------------------------------------------------
    public static void AddMutexGroup(UIWindowGroup group)
    {
        if (!ms_MutexGroups.Contains(group))
        {
            ms_MutexGroups.Add(group);
        }
    }

    //---------------------------------------------------------------------
    public static bool ContainsMutexGroup(UIWindowGroup group)
    {
        return ms_MutexGroups.Contains(group);
    }

    //---------------------------------------------------------------------
    public static void ClearMutexGroup(UIWindowGroup group)
    {
        ms_MutexGroups.Clear();
    }

    //---------------------------------------------------------------------
    public static bool Setup(Transform uiSceneRoot,
        Transform uiManagerRoot, Type waittingWindowType)
    {
        if (ms_Instance != null)
        {
            return true;
        }

        if (uiSceneRoot == null ||
            uiManagerRoot == null ||
            waittingWindowType == null)
        {
            return false;
        }

        if (!waittingWindowType.IsSubclassOf(typeof(UIWaitingBase)) &&
            Array.IndexOf(waittingWindowType.GetInterfaces(), typeof(UIWaitingBase)) < 0)
        {
            DebugHelper.LogError("Waitting window type must be inherit UIWaittingBase interface.");
            return false;
        }

        ms_WaittingType = waittingWindowType;
        UIManager uiManager = uiManagerRoot.GetComponent<UIManager>();

        if (uiManager == null)
        {
            DebugHelper.LogException(new MissingComponentException(
                "Can not found out UIManager component."));
            return false;
        }

        ms_Instance = uiManager;
        ms_MutexGroups.Add(UIWindowGroup.Window);
        AddRollbackGroup(UIWindowGroup.Window);
        Instance.m_InRequestRollback = false;

        AssetManager.LoadObjectWrapper("ui/localization/localization.bytes", 0, null,
            delegate (AssetEventArgs args)
            {
                TextAsset textAsset = args.source as TextAsset;
                Localization.LoadCSV(textAsset.bytes);
                if (Localization.knownLanguages.Length == 0)
                {
                    return;
                }

                string currentLanguage =
                    PlayerPrefs.GetString("Language", "English");
                bool isValideLanguage = false;
                for (int index = 0; index < Localization.knownLanguages.Length; ++index)
                {
                    if (Localization.knownLanguages[index] == currentLanguage)
                    {
                        isValideLanguage = true;
                        break;
                    }
                }
                if (!isValideLanguage)
                {
                    Localization.language = Localization.knownLanguages[0];
                }
                else
                {
                    Localization.language = currentLanguage;
                }
            });
        return true;
    }

    public static bool Initialize(Transform uiTransform, Handler completed)
    {
        if (uiTransform == null || completed == null)
        {
            return false;
        }

        UIManager uiManager = uiTransform.GetComponent<UIManager>();
        if (uiManager == null)
        {
            DebugHelper.LogException(new MissingComponentException(
                "Can not found out UIManager component."));
            return false;
        }

        ms_Instance = uiManager;
        ms_MutexGroups.Add(UIWindowGroup.Window);
        AddRollbackGroup(UIWindowGroup.Window);
        Instance.m_InRequestRollback = false;

        Instance.PrepareAssets(completed);
        return true;
    }

    //---------------------------------------------------------------------
    public static bool CancelShutdown(UIWindow win)
    {
        if (win == null)
        {
            return false;
        }

        return CancelShutdown(win.GetType());
    }

    //---------------------------------------------------------------------
    public static bool CancelShutdown(Type winType)
    {
        if (Instance == null)
        {
            return false;
        }

        return Instance.m_ShutdownWindows.Remove(winType);
    }

    //---------------------------------------------------------------------
    public static bool Shutdown<T>() where T : UIWindow
    {
        return Shutdown(typeof(T));
    }

    //---------------------------------------------------------------------
    public static bool Shutdown(UIWindow win)
    {
        if (win == null)
        {
            return false;
        }

        return Shutdown(win.GetType());
    }

    //---------------------------------------------------------------------
    public static bool Shutdown(Type type)
    {
        if (Instance != null)
        {
            return Instance.DoShutdown(type);
        }

        return false;
    }

    //---------------------------------------------------------------------
    public static void ShutdownGroup(UIWindowGroup group)
    {
        ShutdownGroup(group, null);
    }

    //---------------------------------------------------------------------
    public static void ShutdownGroup(UIWindowGroup group, List<Type> excludeList)
    {
        UIWindow[] windows = Windows(group);
        for (int index = 0; index < windows.Length; ++index)
        {
            Type winType = windows[index].GetType();
            if (excludeList != null && excludeList.Contains(winType))
            {
                continue;
            }

            if (!Shutdown(winType))
            {
                DebugHelper.LogWarning("Shutdown '" + winType.Name + "' failed.");
            }
        }
    }

    //---------------------------------------------------------------------
    public static void ShutdownAll()
    {
        ShutdownAll(null);
    }

    //---------------------------------------------------------------------
    public static void ShutdownAll(List<Type> excludeList)
    {
        for (int index = 0; index < Instance.m_WindowList.Count; ++index)
        {
            Type winType = Instance.m_WindowList[index].GetType();
            if (excludeList != null && excludeList.Contains(winType))
            {
                continue;
            }

            if (!Shutdown(winType))
            {
                DebugHelper.LogWarning("Shutdown '" + winType.Name + "' failed.");
            }
        }
    }

    //---------------------------------------------------------------------
    public static T Window<T>() where T : UIWindow
    {
        return Window(typeof(T)) as T;
    }

    //---------------------------------------------------------------------
    public static UIWindow Window(Type type)
    {
        if (Instance == null)
        {
            return null;
        }

        return Instance.GetWindow(type);
    }

    //---------------------------------------------------------------------
    public static void Window<T>(WindowAsynHandler handler, params object[] data)
    {
        if (Instance == null)
        {
            return;
        }

        Instance.LoadWindow(typeof(T), handler, data);
    }

    //---------------------------------------------------------------------
    public static void Window(Type type, WindowAsynHandler handler, params object[] data)
    {
        if (Instance == null)
        {
            return;
        }

        Instance.LoadWindow(type, handler, data);
    }

    //---------------------------------------------------------------------
    public static UIWindow[] Windows(UIWindowGroup group)
    {
        if (Instance == null)
        {
            return null;
        }

        return Instance.GetWindows(group);
    }

    //---------------------------------------------------------------------
    public static void ShowHide<TShow, THide>()
        where TShow : UIWindow
        where THide : UIWindow
    {
        ShowHide(typeof(TShow), null, typeof(THide), null);
    }

    //---------------------------------------------------------------------
    public static void ShowHide<TShow, THide>(object[] showParam, object[] hideParam)
        where TShow : UIWindow
        where THide : UIWindow
    {
        ShowHide(typeof(TShow), showParam, typeof(THide), hideParam);
    }

    //---------------------------------------------------------------------
    public static void ShowHide(Type showType, Type hideType)
    {
        ShowHide(showType, null, hideType, null);
    }

    //---------------------------------------------------------------------
    public static void ShowHide(Type showType, object[] showParam,
        Type hideType, object[] hideParam)
    {
        Show(showType, delegate (UIWindow win, object data)
        {
            if (win.IsShow())
            {
                Hide(hideType, hideParam);
            }
            else
            {
                DefaultHandler preShowHandler = null;
                preShowHandler = delegate ()
                {
                    win.PreShowEvent -= preShowHandler;
                    Hide(hideType, hideParam);
                };
                win.PreShowEvent += preShowHandler;
            }
        }, showParam);
    }

    //---------------------------------------------------------------------
    public static void Show<T>() where T : UIWindow
    {
        if (Instance != null)
        {
            Instance.DoShow(typeof(T), null, null);
        }
    }

    //---------------------------------------------------------------------
    public static void Show<T>(params object[] data) where T : UIWindow
    {
        if (Instance != null)
        {
            Instance.DoShow(typeof(T), null, data);
        }
    }

    //---------------------------------------------------------------------
    public static void Show<T>(WindowAsynHandler handler,
        params object[] data) where T : UIWindow
    {
        if (Instance != null)
        {
            Instance.DoShow(typeof(T), handler, data);
        }
    }

    //---------------------------------------------------------------------
    public static void Show(Type winType)
    {
        if (Instance != null)
        {
            Instance.DoShow(winType, null, null);
        }
    }

    //---------------------------------------------------------------------
    public static void Show(Type winType, params object[] data)
    {
        if (Instance != null)
        {
            Instance.DoShow(winType, null, data);
        }
    }

    //---------------------------------------------------------------------
    public static void Show(Type winType, WindowAsynHandler handler)
    {
        if (Instance != null)
        {
            Instance.DoShow(winType, handler, null);
        }
    }

    //---------------------------------------------------------------------
    public static void Show(Type winType, WindowAsynHandler handler, params object[] data)
    {
        if (Instance != null)
        {
            Instance.DoShow(winType, handler, data);
        }
    }

    //---------------------------------------------------------------------
    public static void Hide<T>() where T : UIWindow
    {
        if (Instance != null)
        {
            Instance.DoHide(typeof(T), null);
        }
    }

    //---------------------------------------------------------------------
    public static void Hide(Type type)
    {
        if (Instance != null)
        {
            Instance.DoHide(type, null);
        }
    }

    //---------------------------------------------------------------------
    public static void Hide<T>(params object[] data)
    {
        if (Instance != null)
        {
            Instance.DoHide(typeof(T), data);
        }
    }

    //---------------------------------------------------------------------
    public static void Hide(Type type, params object[] data)
    {
        if (Instance != null)
        {
            Instance.DoHide(type, data);
        }
    }

    //---------------------------------------------------------------------
    public static void ShowGroup(UIWindowGroup winGroup)
    {
        ShowGroup(winGroup, null, null);
    }

    //---------------------------------------------------------------------
    public static void ShowGroup(UIWindowGroup winGroup,
        Param1Handler<object> handler, params object[] data)
    {
        List<UIWindow> winList = Instance.m_WindowList;
        int totalWinCount = winList.Count;
        int alreadyWindowCount = 0;
        for (int index = 0; index < winList.Count; ++index)
        {
            UIWindow win = winList[index];
            if (win.windowGroup == winGroup)
            {
                if (handler == null)
                {
                    win.Show();
                    continue;
                }

                DefaultHandler postShowHandler = null;
                postShowHandler = delegate ()
                {
                    win.PostShowEvent -= postShowHandler;
                    alreadyWindowCount++;
                    if (alreadyWindowCount >= totalWinCount)
                    {
                        handler(data);
                    }
                };
                win.PostShowEvent += postShowHandler;
                win.Show();
            }
        }
    }

    //---------------------------------------------------------------------
    public static void ShowAll()
    {
        List<UIWindow> winList = Instance.m_WindowList;
        for (int index = 0; index < winList.Count; ++index)
        {
            winList[index].Show();
        }
    }

    //---------------------------------------------------------------------
    public static void ShowAll(Param1Handler<object> handler, params object[] data)
    {
        List<UIWindow> winList = Instance.m_WindowList;
        int totalWinCount = winList.Count;
        int alreadyWindowCount = 0;
        for (int index = 0; index < winList.Count; ++index)
        {
            UIWindow win = winList[index];
            DefaultHandler postHandler = null;
            postHandler = delegate ()
            {
                win.PostHideEvent -= postHandler;
                alreadyWindowCount++;
                if (alreadyWindowCount >= totalWinCount)
                {
                    handler(data);
                }
            };
            win.PostHideEvent += postHandler;
            win.Show();
        }
    }

    //---------------------------------------------------------------------
    public static void HideGroup(UIWindowGroup winGroup)
    {
        HideGroup(winGroup, null, null, null);
    }

    //---------------------------------------------------------------------
    public static void HideGroup(UIWindowGroup winGroup, Type exclusiveType)
    {
        HideGroup(winGroup, exclusiveType, null, null);
    }

    //---------------------------------------------------------------------
    public static void HideGroup(UIWindowGroup winGroup,
        Param1Handler<object> handler, params object[] data)
    {
        HideGroup(winGroup, null, handler, data);
    }

    //---------------------------------------------------------------------
    public static void HideGroup(UIWindowGroup winGroup, Type exclusiveType,
        Param1Handler<object> handler, params object[] data)
    {
        List<UIWindow> winList = Instance.m_WindowList;
        int totalWinCount = winList.Count;
        int alreadyWindowCount = 0;
        for (int index = 0; index < winList.Count; ++index)
        {
            UIWindow win = winList[index];
            if (win.GetType() == exclusiveType)
            {
                continue;
            }

            if (win.windowGroup == winGroup)
            {
                if (handler == null)
                {
                    win.Hide();
                    continue;
                }

                DefaultHandler postHideHandler = null;
                postHideHandler = delegate ()
                {
                    win.PostHideEvent -= postHideHandler;
                    alreadyWindowCount++;
                    if (alreadyWindowCount >= totalWinCount)
                    {
                        handler(data);
                    }
                };
                win.PostHideEvent += postHideHandler;
                win.Hide();
            }
        }
    }

    //---------------------------------------------------------------------
    public static void HideAll()
    {
        List<UIWindow> winList = Instance.m_WindowList;
        for (int index = 0; index < winList.Count; ++index)
        {
            winList[index].Hide();
        }
    }

    //---------------------------------------------------------------------
    public static void HideAll(Param1Handler<object> handler, params object[] data)
    {
        List<UIWindow> winList = Instance.m_WindowList;
        int totalWinCount = winList.Count;
        int alreadyWindowCount = 0;
        for (int index = 0; index < winList.Count; ++index)
        {
            UIWindow win = winList[index];
            DefaultHandler postHandler = null;
            postHandler = delegate ()
            {
                win.PostHideEvent -= postHandler;
                alreadyWindowCount++;
                if (alreadyWindowCount >= totalWinCount)
                {
                    handler(data);
                }
            };
            win.PostHideEvent += postHandler;
            win.Hide();
        }
    }

    //---------------------------------------------------------------------
    public static void Command<T>() where T : UIWindow
    {
        Command(typeof(T), null);
    }

    //---------------------------------------------------------------------
    public static void Command<T>(params object[] data) where T : UIWindow
    {
        if (Instance != null)
        {
            Instance.DoCommand(typeof(T), data);
        }
    }

    //---------------------------------------------------------------------
    public static void Command(Type type)
    {
        Command(type, null);
    }

    //---------------------------------------------------------------------
    public static void Command(Type type, params object[] data)
    {
        if (Instance != null)
        {
            Instance.DoCommand(type, data);
        }
    }

    //---------------------------------------------------------------------
    public static void Refresh<T>() where T : UIWindow
    {
        Refresh(typeof(T), null);
    }

    //---------------------------------------------------------------------
    public static void Refresh<T>(params object[] data) where T : UIWindow
    {
        if (Instance != null)
        {
            Instance.DoRefresh(typeof(T), data);
        }
    }

    //---------------------------------------------------------------------
    public static void Refresh(Type type)
    {
        Refresh(type, null);
    }

    //---------------------------------------------------------------------
    public static void Refresh(Type type, params object[] data)
    {
        if (Instance != null)
        {
            Instance.DoRefresh(type, data);
        }
    }

    //---------------------------------------------------------------------
    public static bool Search<T>(string conditionType, object conditionValue,
        Param1Handler<Transform> handler, params object[] datas) where T : UIWindow
    {
        return Search(typeof(T),
            conditionType, conditionValue, handler, datas);
    }

    //---------------------------------------------------------------------
    public static bool Search(Type type, string conditionType,
        object conditionValue, Param1Handler<Transform> handler, params object[] datas)
    {
        if (Instance != null)
        {
            return Instance.DoSearch(type,
                conditionType, conditionValue, handler, datas);
        }

        return false;
    }

    //---------------------------------------------------------------------
    public static void AddRollbackGroup(UIWindowGroup group)
    {
        if (Instance == null)
        {
            return;
        }

        if (!Instance.m_RollbackGroupList.Contains(group))
        {
            Instance.m_RollbackGroupList.Add(group);
        }
    }

    //---------------------------------------------------------------------
    public static void RemoveRollbackGroup(UIWindowGroup group)
    {
        if (Instance == null)
        {
            return;
        }

        Instance.m_RollbackGroupList.Remove(group);
    }

    //---------------------------------------------------------------------
    public static void ExcludeRollback<T>() where T : UIWindow
    {
        ExcludeRollback(typeof(T));
    }

    //---------------------------------------------------------------------
    public static void ExcludeRollback(Type winType)
    {
        if (Instance == null || !ValidateWindowType(winType))
        {
            return;
        }

        if (Instance.m_ExcludeRoolbackList.Contains(winType) ||
            Instance.m_ExcludeRoolbackForeverList.Contains(winType))
        {
            return;
        }

        Instance.m_ExcludeRoolbackList.Add(winType);
    }

    //---------------------------------------------------------------------
    public static void IncludeRollback<T>() where T : UIWindow
    {
        IncludeRollback(typeof(T));
    }

    //---------------------------------------------------------------------
    public static void IncludeRollback(Type winType)
    {
        if (Instance == null || !ValidateWindowType(winType))
        {
            return;
        }

        if (!Instance.m_ExcludeRoolbackList.Contains(winType))
        {
            return;
        }

        Instance.m_ExcludeRoolbackList.Remove(winType);
    }

    //---------------------------------------------------------------------
    public static void ExcludeRollbackForever<T>() where T : UIWindow
    {
        ExcludeRollbackForever(typeof(T));
    }

    //---------------------------------------------------------------------
    public static void ExcludeRollbackForever(Type winType)
    {
        if (Instance == null || !ValidateWindowType(winType))
        {
            return;
        }

        if (Instance.m_ExcludeRoolbackForeverList.Contains(winType))
        {
            return;
        }

        Instance.m_ExcludeRoolbackForeverList.Add(winType);
    }

    //---------------------------------------------------------------------
    public static bool IsRollback(Type winType)
    {
        Stack<RollbackInfo>.Enumerator iter =
            Instance.m_RollbackStack.GetEnumerator();
        while (iter.MoveNext())
        {
            RollbackInfo info = iter.Current;
            for (int index = 0; index < info.winList.Count; ++index)
            {
                if (info.winList[index].GetType() == winType)
                {
                    return true;
                }
            }
        }

        return false;
    }

    //---------------------------------------------------------------------
    public static bool IsRollback(UIWindow win)
    {
        Stack<RollbackInfo>.Enumerator iter =
            Instance.m_RollbackStack.GetEnumerator();
        while (iter.MoveNext())
        {
            if (iter.Current.winList.Contains(win))
            {
                return true;
            }
        }

        return false;
    }

    //---------------------------------------------------------------------
    public static List<UIWindow> PeekRoolback()
    {
        if (Instance.m_RollbackStack.Count == 0)
        {
            return null;
        }

        return Instance.m_RollbackStack.Peek().winList;
    }

    //---------------------------------------------------------------------
    public static void ClearRollback()
    {
        if (Instance == null)
        {
            return;
        }

        Instance.m_RollbackStack.Clear();
    }

    //---------------------------------------------------------------------
    public static GameObject Raycast(Vector3 pos)
    {
        if (UICamera.Raycast(pos))
        {
            return UICamera.lastHit.transform.gameObject;
        }

        return null;
    }

    //---------------------------------------------------------------------
    public static bool AsyncInvoke<T>(string memberName,
        params object[] paramList) where T : UIWindow
    {
        return AsyncInvoke(typeof(T), memberName, paramList);
    }

    //---------------------------------------------------------------------
    public static bool AsyncInvoke(Type winType,
        string memberName, params object[] paramList)
    {
        MemberInfo[] memberInfos = winType.GetMember(memberName,
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.Instance);

        if (memberInfos == null || memberInfos.Length == 0)
        {
            DebugHelper.LogError("Can not find member: " +
                winType.Name + "." + memberName);
            return false;
        }

        MemberInfo resultMemberInfo = null;
        for (int i = 0; i < memberInfos.Length; ++i)
        {
            MemberInfo memberInfo = memberInfos[i];
            if (memberInfo.MemberType == MemberTypes.Property)
            {
                PropertyInfo propertyInfo = memberInfo as PropertyInfo;
                if (paramList.Length != 1)
                {
                    continue;
                }

                if (!propertyInfo.CanWrite)
                {
                    continue;
                }

                if (propertyInfo.PropertyType != paramList[0].GetType())
                {
                    continue;
                }

                resultMemberInfo = memberInfo;
            }
            else if (memberInfo.MemberType == MemberTypes.Method)
            {
                MethodInfo methodInfo = memberInfo as MethodInfo;
                ParameterInfo[] parameters = methodInfo.GetParameters();
                if (parameters.Length != paramList.Length)
                {
                    continue;
                }

                if (parameters.Length == 0)
                {
                    resultMemberInfo = memberInfo;
                }
                else
                {
                    for (int index = 0; index < parameters.Length; ++index)
                    {
                        ParameterInfo parameterInfo = parameters[index];
                        if (parameterInfo.ParameterType != paramList[index].GetType())
                        {
                            break;
                        }

                        if (index == parameters.Length - 1)
                        {
                            resultMemberInfo = memberInfo;
                        }
                    }
                }
            }
        }

        if (resultMemberInfo == null)
        {
            DebugHelper.LogError("Can not find to match member info: " +
                winType.Name + "." + memberName);
            return false;
        }


        List<AsyncInvokeInfo> infoList = null;
        if (!ms_AsyncInvokeList.TryGetValue(winType, out infoList))
        {
            infoList = new List<AsyncInvokeInfo>();
            ms_AsyncInvokeList.Add(winType, infoList);
        }
        infoList.Add(new AsyncInvokeInfo(resultMemberInfo, paramList));

        return true;
    }

    //---------------------------------------------------------------------
    public static bool ValidateWindowType(Type winType)
    {
        return winType.IsSubclassOf(typeof(UIWindow));
    }

    //---------------------------------------------------------------------
    public static int GetShowCount(UIWindowGroup group, bool includeHiding)
    {
        if (Instance == null)
        {
            return 0;
        }

        List<UIWindow> wins = Instance.m_WindowList;
        int currentShowCount = 0;
        for (int index = 0; index < wins.Count; ++index)
        {
            UIWindow win = wins[index];
            if (win.windowGroup == group && win.IsShow())
            {
                if (!includeHiding && win.IsHiding())
                {
                    continue;
                }

                currentShowCount++;
            }
        }

        return currentShowCount;
    }
    #endregion

    #region Internal Method
    //---------------------------------------------------------------------
    private void ProcessAsyncInvokeList()
    {
        for (ms_AsyncInvokeList.Begin(); ms_AsyncInvokeList.Next();)
        {
            Type winType = ms_AsyncInvokeList.Key;
            UIWindow win = GetWindow(winType);
            if (win == null)
            {
                continue;
            }

            List<AsyncInvokeInfo> infoList = ms_AsyncInvokeList.Value;
            for (int index = 0; index < infoList.Count; ++index)
            {
                AsyncInvokeInfo info = infoList[index];
                MemberInfo memberInfo = info.memberInfo;
                object[] paramList = info.paramList;

                try
                {
                    if (memberInfo.MemberType == MemberTypes.Property)
                    {
                        PropertyInfo propertyInfo = memberInfo as PropertyInfo;
                        propertyInfo.SetValue(win, paramList[0], null);
                    }
                    else
                    {
                        MethodInfo methodInfo = memberInfo as MethodInfo;
                        methodInfo.Invoke(win, paramList);
                    }
                }
                catch (System.Exception ex)
                {
                    DebugHelper.LogError("Async invoke failed: " + ex.Message);
                }
            }

            infoList.Clear();
        }
    }

    private void PrepareAssets(Handler completeHandler)
    {
        AssetQueueLoader uiAssetLoader = new AssetQueueLoader();
        string localAssetPath = "Localization/Localization";
        AssetBundleManager.LoadAsset(localAssetPath, delegate (UnityEngine.Object ret)
         {
             if (ret == null)
                 throw new Exception("语言本地化初始化失败");
             else
             {
                 TextAsset textAsset = ret as TextAsset;
                 Localization.LoadCSV(textAsset.bytes);
                 if (Localization.knownLanguages.Length == 0)
                 {
                     return;
                 }

                 string currentLanguage =
                     PlayerPrefs.GetString("Language", "English");
                 bool isValideLanguage = false;
                 for (int index = 0; index < Localization.knownLanguages.Length; ++index)
                 {
                     if (Localization.knownLanguages[index] == currentLanguage)
                     {
                         isValideLanguage = true;
                         break;
                     }
                 }
                 if (!isValideLanguage)
                     Localization.language = Localization.knownLanguages[0];
                 else
                     Localization.language = currentLanguage;
             }

             if (completeHandler != null)
                 completeHandler();
         });

        //if (ms_WaiterType != null)
        //{
        //    string waitAssetPath = Instance.GetWindowPath(ms_WaiterType);
        //    uiAssetLoader.Request(waitAssetPath, 1, null, delegate (AssetEventArgs args)
        //    {
        //        // Failed
        //        if (args.result == null)
        //        {
        //            DebugHelper.LogWarning(ms_WaiterType.Name + " load failed.");
        //        }
        //        else
        //        // Succeed
        //        {
        //            GameObject winGo = args.result as GameObject;
        //            winGo.name = ms_WaiterType.Name;
        //            CoreUtility.AttachChild(windowRoot, winGo.transform);
        //            CoreUtility.NormalizeTransform(winGo);
        //        }
        //    });
        //}

        
    }


    //---------------------------------------------------------------------
    private void LoadWindow(Type type, WindowAsynHandler handler, object[] data)
    {
        if (type == null)
            return;
        UIWindow win = GetWindow(type);
        if (win != null && win.IsInitialized())
        {
            if (handler != null)
            {
                handler(win, data);
            }

            return;
        }

        List<PendingInfo> pendingInfoList = null;
        if (!m_PendingWindows.TryGetValue(type, out pendingInfoList))
        {
            pendingInfoList = new List<PendingInfo>();
            m_PendingWindows.Add(type, pendingInfoList);

            if (handler != null)
            {
                pendingInfoList.Add(new PendingInfo(handler, data));
            }
        }
        else
        {
            if (handler != null)
            {
                pendingInfoList.Add(new PendingInfo(handler, data));
            }

            return;
        }

        AssetBundleManager.LoadAsset(GetWindowPath(type).ToLower(), type.Name.ToLower(),
            delegate (UnityEngine.Object ret)
            {
                // Failed
                if (ret == null)
                {
                    DebugHelper.LogWarning(type.Name + " load failed.");
                    if (handler != null)
                    {
                        handler(null, data);
                    }
                }
                else
                {
                    // Succeed
                    GameObject winGo = GameObject.Instantiate(ret as GameObject);
                    winGo.name = type.Name;
                    CoreUtility.AttachChild(windowRoot, winGo.transform);
                    CoreUtility.NormalizeTransform(winGo);
                }
            });

        //AssetManager.LoadObjectWrapper(GetWindowPath(type), 1, null, null,
        //    delegate(AssetEventArgs args)
        //    {
        //        // Failed
        //        if (args.result == null)
        //        {
        //            DebugHelper.LogWarning(type.Name + " load failed.");
        //            if (handler != null)
        //            {
        //                handler(null, data);
        //            }
        //        }
        //        else
        //        {
        //            // Succeed
        //            GameObject winGo = args.result as GameObject;
        //            winGo.name = type.Name;
        //            CoreUtility.AttachChild(windowRoot, winGo.transform);
        //            CoreUtility.NormalizeTransform(winGo);
        //        }
        //    });
    }

    //---------------------------------------------------------------------
    private void DoShow(Type type, WindowAsynHandler handler, object[] data)
    {
        m_RequestShows.Add(type);
        LoadWindow(type, delegate (UIWindow win, object data1)
        {
            if (handler != null)
            {
                handler(win, data);
            }

            m_RequestShows.Remove(type);
            win.InternalShow(data);
        }, data);
    }

    //---------------------------------------------------------------------
    private void DoHide(Type type, object[] datas)
    {
        m_RequestShows.Remove(type);
        UIWindow win = GetWindow(type);
        if (win != null)
        {
            win.InternalHide(datas);
        }
    }

    //---------------------------------------------------------------------
    private void DoRefresh(Type type, object[] datas)
    {
        LoadWindow(type, delegate (UIWindow win, object data1)
        {
            win.InternalRefresh(datas);
        }, datas);
    }

    //---------------------------------------------------------------------
    private void DoCommand(Type type, object[] datas)
    {
        LoadWindow(type, delegate (UIWindow win, object data1)
        {
            win.InternalCommand(datas);
        }, datas);
    }

    //---------------------------------------------------------------------
    private bool DoSearch(Type type, string conditionType,
        object conditionValue, Param1Handler<Transform> handler, object[] datas)
    {
        LoadWindow(type, delegate (UIWindow win, object data1)
        {
            win.InternalSearch(conditionType, conditionValue, handler, datas);
        }, new object[] { conditionType, conditionValue, handler, datas });

        return true;
    }

    //---------------------------------------------------------------------
    private bool DoShutdown(Type type)
    {
        bool result = false;
        m_RequestShows.Remove(type);
        List<PendingInfo> pendingInfoList = null;
        if (m_PendingWindows.TryGetValue(type, out pendingInfoList))
        {
            for (int index = 0; index < pendingInfoList.Count; ++index)
            {
                PendingInfo pendingInfo = pendingInfoList[index];
                if (pendingInfo.handler != null)
                {
                    pendingInfo.handler(null, pendingInfo.data);
                }
            }

            result = m_PendingWindows.Remove(type);
        }
        else
        {
            UIWindow win = Window(type);
            if (win != null)
            {
                result = true;
                win.Shutdown();
            }
        }

        return result;
    }

    //---------------------------------------------------------------------
    private void AddWindow(UIWindow win)
    {
        if (m_WindowList.Contains(win))
        {
            DebugHelper.LogWarning(win.GetType().Name +
                " is the Instance, only one instance.");
            return;
        }

        m_WindowList.Add(win);
    }

    //---------------------------------------------------------------------
    private UIWindow GetWindow(Type type)
    {
        for (int i = 0; i < m_WindowList.Count; ++i)
        {
            UIWindow win = m_WindowList[i];
            if (win.GetType() == type)
            {
                return win;
            }
        }

        return null;
    }

    //---------------------------------------------------------------------
    private UIWindow[] GetWindows(UIWindowGroup group)
    {
        List<UIWindow> windows = new List<UIWindow>();
        for (int index = 0; index < Instance.m_WindowList.Count; ++index)
        {
            UIWindow win = Instance.m_WindowList[index];
            if (win.windowGroup == group)
            {
                windows.Add(win);
            }
        }

        return windows.ToArray();
    }

    //---------------------------------------------------------------------
    private string GetWindowPath(Type type)
    {
        string winAssetPath = string.Empty;
        if (m_WinAssetPathMap.TryGetValue(type, out winAssetPath) &&
            winAssetPath != string.Empty)
        {
            return winAssetPath;
        }

        winAssetPath = PathHelper.Combine(windowFolder, type.Name);
        if (m_WinAssetPathMap.ContainsKey(type))
        {
            m_WinAssetPathMap[type] = winAssetPath;
        }
        else
        {
            m_WinAssetPathMap.Add(type, winAssetPath);
        }

        return winAssetPath;
    }

    //---------------------------------------------------------------------
    private void RequestPushRollback(UIWindow currentWin)
    {
        if (m_InRequestRollback)
        {
            return;
        }

        m_InRequestRollback = true;
        if (!CanRollback(currentWin))
        {
            m_InRequestRollback = false;
            return;
        }

        if (m_RollbackStack.Count != 0 &&
            m_RollbackStack.Peek().source == currentWin)
        {
            m_InRequestRollback = false;
            return;
        }

        List<UIWindow> winList = new List<UIWindow>();
        for (int i = 0; i < m_WindowList.Count; ++i)
        {
            UIWindow lastWin = m_WindowList[i];
            if (lastWin != currentWin && lastWin.IsShow() &&
                m_RollbackGroupList.Contains(lastWin.windowGroup))
            {
                winList.Add(lastWin);
            }
        }

        if (winList.Count != 0)
        {
            RollbackInfo info = new RollbackInfo(currentWin, winList);
            m_RollbackStack.Push(info);

            DefaultHandler preShowHandler = null;
            preShowHandler = delegate ()
            {
                bool lastState = m_InRequestRollback;
                m_InRequestRollback = true;
                currentWin.PreShowEvent -= preShowHandler;
                for (int i = 0; i < info.winList.Count; ++i)
                {
                    UIWindow rollbackWin = info.winList[i];
                    rollbackWin.Hide((object[])rollbackWin.NotifyPushRollback());
                }
                m_InRequestRollback = lastState;
            };
            currentWin.PreShowEvent += preShowHandler;
        }

        m_InRequestRollback = false;
    }

    //---------------------------------------------------------------------
    private void RequestPopRollback(UIWindow currentWin)
    {
        if (m_InRequestRollback || m_RollbackStack.Count == 0)
        {
            return;
        }

        m_InRequestRollback = true;
        RollbackInfo info = m_RollbackStack.Peek();
        if (!CanRollback(currentWin) || info.source != currentWin)
        {
            m_InRequestRollback = false;
            return;
        }

        int remainShowCount = info.winList.Count;
        currentWin.CancelHide();
        for (int i = 0; i < info.winList.Count; ++i)
        {
            UIWindow rollbackWin = info.winList[i];
            DefaultHandler preShowHandler = null;
            preShowHandler = delegate ()
            {
                currentWin.PreShowEvent -= preShowHandler;
                --remainShowCount;
                if (remainShowCount == 0)
                {
                    bool lastState = m_InRequestRollback;
                    m_InRequestRollback = true;
                    currentWin.Hide();
                    m_InRequestRollback = lastState;
                }
            };
            rollbackWin.PreShowEvent += preShowHandler;

            if (rollbackWin.IsShow())
            {
                preShowHandler();
            }
            else
            {
                rollbackWin.Show((object[])rollbackWin.NotifyPopRollback());
            }
        }

        m_InRequestRollback = false;
    }

    //---------------------------------------------------------------------
    private void ExecutePopRollback(UIWindow currentWin)
    {
        if (m_RollbackStack.Count == 0 ||
            m_RollbackStack.Peek().source != currentWin)
        {
            return;
        }

        m_RollbackStack.Pop();
    }

    //---------------------------------------------------------------------
    private bool CanRollback(UIWindow win)
    {
        if (win == null)
        {
            return false;
        }

        if (!m_RollbackGroupList.Contains(win.windowGroup))
        {
            return false;
        }

        if (m_ExcludeRoolbackList.Contains(win.GetType()) ||
            m_ExcludeRoolbackForeverList.Contains(win.GetType()))
        {
            return false;
        }

        return true;
    }

    //---------------------------------------------------------------------
    private void ProcessShutdownWindows()
    {
        for (int index = 0; index < m_ShutdownWindows.Count; ++index)
        {
            Type winType = m_ShutdownWindows[index];
            UIWindow win = GetWindow(winType);
            if (win == null || win.IsShow())
            {
                continue;
            }

            if (WindowShutdownEvent != null)
            {
                WindowShutdownEvent(win);
            }

            win.DoShutdown();

            m_WindowList.Remove(win);
            m_ShutdownWindows.Remove(win.GetType());
            --index;
            CoreUtility.Destroy(win.gameObject);
        }
    }
    #endregion

    #region Unity Method
    //---------------------------------------------------------------------
    private void Awake()
    {
        ms_SceneObject = gameObject;
    }

    //---------------------------------------------------------------------
    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.F11))
        {
            if (UICamera.mainCamera != null)
            {
                UICamera.mainCamera.enabled = !UICamera.mainCamera.enabled;
            }
        }

        for (int index = 0; index < m_WindowList.Count; ++index)
        {
            m_WindowList[index].ProcessRequests();
        }

        ProcessAsyncInvokeList();

        if (m_ShutdownWindows.Count != 0)
        {
            ProcessShutdownWindows();
        }
    }
    #endregion

    #region Internal Declare
    //---------------------------------------------------------------------
    private struct PendingInfo
    {
        public object[] data;
        public WindowAsynHandler handler;

        public PendingInfo(WindowAsynHandler handler, object[] data)
        {
            this.data = data;
            this.handler = handler;
        }
    }

    //---------------------------------------------------------------------
    private struct ShowInfo
    {
        public Type type;
        public object[] data;
        public UIWindow win;
        public WindowAsynHandler handler;

        public ShowInfo(Type type, WindowAsynHandler handler, object[] data)
        {
            this.type = type;
            this.data = data;
            this.handler = handler;
            this.win = null;
        }
    }

    //---------------------------------------------------------------------
    private struct AsyncInvokeInfo
    {
        public MemberInfo memberInfo;
        public object[] paramList;

        public AsyncInvokeInfo(MemberInfo mi, object[] pl)
        {
            memberInfo = mi;
            paramList = pl;
        }
    }

    //---------------------------------------------------------------------
    private struct RollbackInfo
    {
        public List<UIWindow> winList;
        public UIWindow source;

        public RollbackInfo(UIWindow source)
        {
            this.source = source;
            this.winList = new List<UIWindow>();
        }

        public RollbackInfo(UIWindow source, List<UIWindow> winList)
        {
            this.source = source;
            this.winList = winList;
        }
    }
    #endregion

    #region Internal Member
    //---------------------------------------------------------------------
    private static GameObject ms_SceneObject = null;
    private static UIManager ms_Instance = null;
    private static Type ms_WaittingType = null;
    private static List<UIWindowGroup> ms_MutexGroups = new List<UIWindowGroup>();
    private static Map<Type, List<AsyncInvokeInfo>> ms_AsyncInvokeList =
        new Map<Type, List<AsyncInvokeInfo>>();

    //---------------------------------------------------------------------
    private bool m_InRequestRollback = false;
    private List<UIWindowGroup> m_RollbackGroupList = new List<UIWindowGroup>();
    private List<Type> m_ExcludeRoolbackList = new List<Type>();
    private List<Type> m_ExcludeRoolbackForeverList = new List<Type>();

    //---------------------------------------------------------------------
    private List<UIWindow> m_WindowList = new List<UIWindow>();
    private List<Type> m_ShutdownWindows = new List<Type>();
    private Stack<RollbackInfo> m_RollbackStack = new Stack<RollbackInfo>();
    private Map<Type, string> m_WinAssetPathMap = new Map<Type, string>();
    private List<Type> m_RequestShows = new List<Type>();
    private Map<Type, List<PendingInfo>> m_PendingWindows =
        new Map<Type, List<PendingInfo>>();
    #endregion
}
