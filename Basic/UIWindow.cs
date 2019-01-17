using System;
using System.Collections.Generic;
using UnityEngine;

    //=========================================================================
[System.Serializable]
public enum UIWindowGroup
{
    HUD = 0,    // lower group
    Window,     // window group, only one show
    Popup,      // window group, only one show
    MsgBox,     // window group, only one show
    Topmost     // notify group
}

    //=========================================================================
public enum UIShutdownMode
{
    Auto,
    Hide,
    Custom
}

    //=========================================================================
public delegate void WindowAsynHandler(UIWindow win, object data);

    //=========================================================================
[RequireComponent(typeof(UIPanel))]
public abstract class UIWindow : UIDataSource
{
    #region Public Property
    //---------------------------------------------------------------------
    [SerializeField]
    protected UIWindowGroup m_WindowGroup = UIWindowGroup.Window;
    public UIWindowGroup windowGroup
    {
        get { return m_WindowGroup; }
    }

    //---------------------------------------------------------------------
    [SerializeField]
    protected UIShutdownMode m_ShutdownMode = UIShutdownMode.Auto;
    public UIShutdownMode shutdownMode
    {
        get { return m_ShutdownMode; }
    }

    //---------------------------------------------------------------------
    protected string m_WaitGroup = null;
    public string defaultWaitGroup
    {
        get
        {
            if (m_WaitGroup == null)
            {
                m_WaitGroup = transform.name + "_DefaultGroup";
            }

            return m_WaitGroup;
        }
    }

    //---------------------------------------------------------------------
    protected string m_LoadingWaitGroup = null;
    public string loadingWaitGroup
    {
        get
        {
            if (m_LoadingWaitGroup == null)
            {
                m_LoadingWaitGroup = transform.name + "_LoadingGroup";
            }

            return m_LoadingWaitGroup;
        }
    }

    #endregion

    #region Public Event
    // The first static asset loaded trigger this message.
    public event DefaultHandler PreparedEvent;

    // The first all asset loaded to touch this event
    public event DefaultHandler InitializedEvent;

    // After all asset have been ready, refresh requests processed.
    public event DefaultHandler RefreshEvent;

    // After all asset have been ready, command requests processed.
    public event DefaultHandler CommandEvent;

    // After all asset have been ready, show prepared event.
    public event DefaultHandler PreShowEvent;

    // After all asset have been ready, show completed event.
    public event DefaultHandler PostShowEvent;

    // After all animation have been completed, hide prepared event.
    public event DefaultHandler PreHideEvent;

    // After all animation have been completed, hide completed event.
    public event DefaultHandler PostHideEvent;

    // Each frame update event.
    public event DefaultHandler UpdateEvent;
    public event DefaultHandler LateUpdateEvent;

    // This event is fired before the destroy of asset.
    public event DefaultHandler ShutdownEvent;
    #endregion

    #region Public Method
    //---------------------------------------------------------------------
    public void Wait(float threshold, float timeout, string note,
        string message, int buttonMask, DefaultHandler retry, DefaultHandler ok)
    {
        UIManager.Wait(defaultWaitGroup, threshold, timeout, note, message, buttonMask, retry, ok);
    }

    //---------------------------------------------------------------------
    public void Dialog(string message, int buttonMask,
        DefaultHandler retryHandler, DefaultHandler okHandler)
    {
        UIManager.Dialog(defaultWaitGroup, message, buttonMask, retryHandler, okHandler);
    }

    //---------------------------------------------------------------------
    public void StopWait()
    {
        UIManager.StopWait(defaultWaitGroup);
    }

    //---------------------------------------------------------------------
    public void Shutdown()
    {
        Hide();
        UIManager.TouchWindowShutdownEvent(this);
    }

    //---------------------------------------------------------------------
    internal void DoShutdown()
    {
        if (ShutdownEvent != null)
        {
            ShutdownEvent();
        }

        OnShutdown();
        StopWait();
    }

    //---------------------------------------------------------------------
    public bool IsInitialized()
    {
        return m_IsInitialized;
    }

    //---------------------------------------------------------------------
    public bool IsCompleted()
    {
        return (m_RequestBatchs.Count == 0);
    }

    //---------------------------------------------------------------------
    public bool IsShowing()
    {
        return ContainsRequestBatch(RequestType.ShowBegin) ||
            ContainsRequestBatch(RequestType.ShowEnd) ||
            ContainsRequestBatch(RequestType.ShowAnimation);
    }

    //---------------------------------------------------------------------
    public bool IsHiding()
    {
        return ContainsRequestBatch(RequestType.HideBegin) ||
            ContainsRequestBatch(RequestType.HideEnd) ||
            ContainsRequestBatch(RequestType.HideAnimation);
    }

    //---------------------------------------------------------------------
    public bool IsShow()
    {
        return gameObject.activeSelf;
    }

    //---------------------------------------------------------------------
    public bool CancelShow()
    {
        if (IsShow())
        {
            return true;
        }

        return RemoveRequestBatch(RequestType.ShowBegin);
    }

    //---------------------------------------------------------------------
    public bool CancelHide()
    {
        if (!IsShow())
        {
            return true;
        }

        return RemoveRequestBatch(RequestType.HideBegin);
    }

    //---------------------------------------------------------------------
    public void Show(params object[] datas)
    {
        InternalShow(datas);
    }

    //---------------------------------------------------------------------
    internal void InternalShow(object[] datas)
    {
        if (IsShow())
        {
            // Cancel hide request
            RemoveRequestBatch(RequestType.HideBegin);
            RemoveRequestBatch(RequestType.HideEnd);
            UIManager.CancelShutdown(this);
            return;
        }
        UIManager.CancelShutdown(this);

        // Refresh first, Filling the data before displaying the content 
        // is not displayed together, avoid the problem.
        InternalRefresh(datas);

        PushRequest(RequestType.ShowBegin, datas);
    }

    //---------------------------------------------------------------------
    public void Hide(params object[] datas)
    {
        InternalHide(datas);
    }

    //---------------------------------------------------------------------
    internal void InternalHide(object[] datas)
    {
        if (!IsShow())
        {
            // Cancel show and refresh request
            RemoveRequestBatch(RequestType.ShowBegin);
            RemoveRequestBatch(RequestType.ShowEnd);
            RemoveRequestBatch(RequestType.Refresh);
            return;
        }

        PushRequest(RequestType.HideBegin, datas);
    }

    //---------------------------------------------------------------------
    public void Refresh(params object[] datas)
    {
        PushRequest(RequestType.Refresh, datas);
    }

    //---------------------------------------------------------------------
    internal void InternalRefresh(object[] datas)
    {
        PushRequest(RequestType.Refresh, datas);
    }

    //---------------------------------------------------------------------
    public void Command(params object[] datas)
    {
        PushRequest(RequestType.Command, datas);
    }

    //---------------------------------------------------------------------
    internal void InternalCommand(object[] datas)
    {
        PushRequest(RequestType.Command, datas);
    }

    //---------------------------------------------------------------------
    public bool Search(string conditionType, object conditionValue,
        Param1Handler<Transform> handler, params object[] datas)
    {
        return InternalSearch(conditionType, conditionValue, handler, datas);
    }

    //---------------------------------------------------------------------
    internal bool InternalSearch(string conditionType, object conditionValue,
        Param1Handler<Transform> handler, object[] datas)
    {
        if (handler == null)
        {
            return false;
        }

        SearchInfo searchInfo = new SearchInfo(
            conditionType, conditionValue, handler);

        if (datas == null || datas.Length == 0)
        {
            PushRequest(RequestType.Search, searchInfo);
        }
        else
        {
            object[] newDatas = new object[datas.Length + 1];
            datas.CopyTo(newDatas, 0);
            newDatas[newDatas.Length - 1] = searchInfo;
            PushRequest(RequestType.Search, newDatas);
        }

        return true;
    }

    //---------------------------------------------------------------------
    public void RequestLoad(string assetPath)
    {
        RequestLoad(assetPath, 1);
    }

    //---------------------------------------------------------------------
    public void RequestLoad(string assetPath, int count)
    {
        RequestLoad(assetPath, count, null);
    }

    //---------------------------------------------------------------------
    public void RequestLoad(string assetPath, int count, AssetEventHandler handler)
    {
        RequestLoad(assetPath, count, null, handler);
    }

    //---------------------------------------------------------------------
    public void RequestLoad(string assetPath, int count, object data, AssetEventHandler handler)
    {
        m_QueueLoader.Request(assetPath, count, data, handler);
        PushRequest(RequestType.Load, requestDataArray);
    }

    //---------------------------------------------------------------------
    public void RequestLoad(string assetPath, UITexture uiTexture)
    {
        RequestLoad(assetPath, 1, delegate(AssetEventArgs args)
        {
            Texture texture = args.source as Texture;
            if (texture != null)
            {
                uiTexture.mainTexture = texture;
            }
        });
    }

    //---------------------------------------------------------------------
    public void BeginLoad()
    {
        BeginLoad(null);
    }

    //---------------------------------------------------------------------
    public void BeginLoad(DefaultHandler completedHandler)
    {
        if (IsShow() && ContainsRequestBatch(RequestType.Load))
        {
            UIManager.Wait(loadingWaitGroup, 1f, 0f,
                "WaitWin_Loading_Note", string.Empty, 0, null, null);
        }

        m_QueueLoader.Begin(completedHandler);
    }

    //---------------------------------------------------------------------
    public override void RefreshDataBinders(UIDataRefreshRate refreshType)
    {
        if (m_DataSources == null)
        {
            return;
        }

        for (int index = 0; index < m_DataSources.Length; ++index)
        {
            UIDataSource dataSource = m_DataSources[index];
            if (dataSource == this)
            {
                continue;
            }

            dataSource.RefreshDataBinders(refreshType);
        }

        base.RefreshDataBinders(refreshType);
    }

    //---------------------------------------------------------------------
    internal object NotifyPushRollback()
    {
        return OnRequestHideByRollback();
    }

    //---------------------------------------------------------------------
    internal object NotifyPopRollback()
    {
        return OnRequestShowByRollback();
    }

    //---------------------------------------------------------------------
    public int CorrectDepth(int depth)
    {
        return CorrectDepth(depth, false);
    }

    //---------------------------------------------------------------------
    public int CorrectDepth(int depth, bool warning)
    {
        int minDepth = GetPanelDepth(windowGroup);
        int maxDepth = GetPanelDepth((UIWindowGroup)(windowGroup + 1)) - 1;
        return CorrectDepth(depth, minDepth, maxDepth, warning);
    }
    #endregion

    #region Internal Property
    //---------------------------------------------------------------------
    protected object[] requestDataArray
    {
        get
        {
            if (m_RequestData == null ||
                m_RequestData.Length == 0)
            {
                return null;
            }

            return m_RequestData;
        }
    }

    //---------------------------------------------------------------------
    protected object requestData
    {
        get
        {
            if (m_RequestData == null ||
                m_RequestData.Length == 0)
            {
                return null;
            }

            if (m_RequestData != null &&
                m_RequestData.Length == 1)
            {
                return m_RequestData[0];
            }

            return m_RequestData;
        }
    }
    #endregion

    #region Interface Method
    //---------------------------------------------------------------------
    protected virtual bool OnPrepared()
    {
        return true;
    }

    //---------------------------------------------------------------------
    protected virtual bool OnInitialized()
    {
        return true;
    }

    //---------------------------------------------------------------------
    protected virtual void OnLoaded()
    {

    }

    //---------------------------------------------------------------------
    protected virtual void OnRefresh()
    {

    }

    //---------------------------------------------------------------------
    protected virtual void OnCommand()
    {

    }

    //---------------------------------------------------------------------
    protected virtual void OnRequestShow()
    {

    }

    //---------------------------------------------------------------------
    protected virtual void OnPreShow()
    {

    }

    //---------------------------------------------------------------------
    protected virtual void OnPostShow()
    {

    }

    //---------------------------------------------------------------------
    protected virtual void OnRequestHide()
    {

    }

    //---------------------------------------------------------------------
    protected virtual void OnPreHide()
    {

    }

    //---------------------------------------------------------------------
    protected virtual void OnPostHide()
    {

    }

    //---------------------------------------------------------------------
    protected virtual void OnUpdate()
    {

    }

    //---------------------------------------------------------------------
    protected virtual void OnLateUpdate()
    {

    }

    //---------------------------------------------------------------------
    protected virtual void OnShutdown()
    {

    }

    //---------------------------------------------------------------------
    protected virtual object OnRequestHideByRollback()
    {
        return null;
    }

    //---------------------------------------------------------------------
    protected virtual object OnRequestShowByRollback()
    {
        return null;
    }

    //---------------------------------------------------------------------
    protected virtual Transform DoSearch(
        string conditionType, object conditionValue)
    {
        return null;
    }
    #endregion

    #region Internal Method
    //---------------------------------------------------------------------
    private void PushRequest(RequestType type, object data)
    {
        PushRequest(type, new object[] { data });
    }

    //---------------------------------------------------------------------
    private void PushRequest(RequestType type, object[] datas)
    {
        if (type == RequestType.None)
        {
            return;
        }

        RequestBatch batch = GetRequestBatch(type);
        if (batch == null)
        {
            batch = new RequestBatch(type);
            m_RequestBatchs.Add(batch);
            m_RequestBatchs.Sort(delegate(RequestBatch a, RequestBatch b)
            {
                return -a.type.CompareTo(b.type);
            });
        }

        string group = defaultGroup;
        if (datas != null && datas.Length != 0)
        {
            List<IUIRequestOption> options = ExtractRequestOption(ref datas);
            string groupOption = GetRequestGroup(options);
            if (!string.IsNullOrEmpty(groupOption))
            {
                group = groupOption;
            }
        }

        if (ContainsRequestGroup(batch, group))
        {
            ModifyRequestGroup(batch, group, datas);
            return;
        }

        // modify mutex request first
        if (type == RequestType.HideBegin)
        {
            if (ModifyMutexRequest(RequestType.HideEnd, group, datas) ||
                ModifyMutexRequest(RequestType.HideAnimation, group, datas))
            {
                return;
            }

            OnRequestHide();
            UIManager.TouchWindowRequestHide(this);
        }
        else if (type == RequestType.ShowBegin)
        {
            if (ModifyMutexRequest(RequestType.ShowEnd, group, datas) ||
                ModifyMutexRequest(RequestType.ShowAnimation, group, datas))
            {
                return;
            }

            OnRequestShow();
            UIManager.TouchWindowRequestShow(this);
        }

        batch.groups.Add(new RequestGroup(group, datas));
    }

    //---------------------------------------------------------------------
    private bool ModifyMutexRequest(RequestType type, string group, object[] datas)
    {
        RequestBatch batch = GetRequestBatch(type);
        if (batch == null)
        {
            return false;
        }

        RequestGroup requestGroup = GetRequestGroup(batch, group);
        if (requestGroup == null)
        {
            return false;
        }

        requestGroup.datas = datas;

        return true;
    }

    //---------------------------------------------------------------------
    private List<IUIRequestOption> ExtractRequestOption(ref object[] datas)
    {
        List<IUIRequestOption> options = new List<IUIRequestOption>();
        List<object> newDatas = new List<object>();
        for (int i = 0; i < datas.Length; ++i)
        {
            object data = datas[i];
            if (data is IUIRequestOption)
            {
                options.Add(data as IUIRequestOption);
            }
            else
            {
                newDatas.Add(data);
            }
        }

        datas = newDatas.ToArray();

        return options;
    }

    //---------------------------------------------------------------------
    private string GetRequestGroup(List<IUIRequestOption> options)
    {
        for (int i = 0; i < options.Count; ++i)
        {
            IUIRequestOption option = options[i];
            if (option is UIRequestGroup)
            {
                UIRequestGroup group = (UIRequestGroup)option;
                return group.name;
            }
        }

        return string.Empty;
    }

    //---------------------------------------------------------------------
    private RequestBatch LastRequestBatch()
    {
        RequestBatch batch = null;
        int index = m_RequestBatchs.Count;
        for (int i = index; i > 0; --i)
        {
            batch = m_RequestBatchs[i - 1];
            if (batch.groups.Count != 0)
            {
                return batch;
            }
            else
            {
                m_RequestBatchs.Remove(batch);
            }
        }

        return null;
    }

    //---------------------------------------------------------------------
    private RequestGroup LastRequestGroup(RequestBatch batch)
    {
        if (batch == null || batch.groups.Count == 0)
        {
            return null;
        }

        return batch.groups[batch.groups.Count - 1];
    }

    //---------------------------------------------------------------------
    private RequestGroup PopRequestGroup()
    {
        RequestBatch batch = LastRequestBatch();
        if (batch == null)
        {
            return null;
        }

        RequestGroup group = LastRequestGroup(batch);
        batch.groups.Remove(group);
        if (batch.groups.Count == 0)
        {
            m_RequestBatchs.Remove(batch);
        }

        return group;
    }

    //---------------------------------------------------------------------
    private RequestBatch GetRequestBatch(RequestType type)
    {
        for (int i = 0; i < m_RequestBatchs.Count; ++i)
        {
            RequestBatch batch = m_RequestBatchs[i];
            if (batch.type == type)
            {
                return batch;
            }
        }

        return null;
    }

    //---------------------------------------------------------------------
    private RequestGroup GetRequestGroup(RequestType type, string group)
    {
        return GetRequestGroup(GetRequestBatch(type), group);
    }

    //---------------------------------------------------------------------
    private RequestGroup GetRequestGroup(RequestBatch batch, string group)
    {
        if (batch == null)
        {
            return null;
        }

        for (int i = 0; i < batch.groups.Count; ++i)
        {
            RequestGroup reqeustGroup = batch.groups[i];
            if (reqeustGroup.group == group)
            {
                return reqeustGroup;
            }
        }

        return null;
    }

    //---------------------------------------------------------------------
    private bool ContainsRequestBatch(RequestType type)
    {
        return GetRequestBatch(type) != null;
    }

    //---------------------------------------------------------------------
    private bool ContainsRequestGroup(RequestType type, string group)
    {
        return GetRequestGroup(type, group) != null;
    }

    //---------------------------------------------------------------------
    private bool ContainsRequestGroup(RequestBatch batch, string group)
    {
        return GetRequestGroup(batch, group) != null;
    }

    //---------------------------------------------------------------------
    private bool ModifyRequestGroup(RequestType type, string group, object[] data)
    {
        return ModifyRequestGroup(GetRequestBatch(type), group, data);
    }

    //---------------------------------------------------------------------
    private bool ModifyRequestGroup(RequestBatch batch, string group, object[] data)
    {
        RequestGroup requestGroup = GetRequestGroup(batch, group);
        if (requestGroup == null)
        {
            return false;
        }

        requestGroup.datas = data;

        return true;
    }

    //---------------------------------------------------------------------
    private bool RemoveRequestBatch(RequestType type)
    {
        RequestBatch batch = GetRequestBatch(type);
        if (batch == null)
        {
            return false;
        }

        return m_RequestBatchs.Remove(batch);
    }

    //---------------------------------------------------------------------
    internal void ProcessRequests()
    {
        // There is not any request.
        RequestBatch batch = LastRequestBatch();
        if (batch == null)
        {
            return;
        }

        // Waiting for animation finished.
        if (batch.type == RequestType.ShowAnimation ||
            batch.type == RequestType.HideAnimation)
        {
            return;
        }

        // Waiting for asset loaded.
        if (batch.type == RequestType.Load)
        {
            if (m_QueueLoader.isCompleted)
            {
                // Pop last group
                PopRequestGroup();
                OnLoaded();
                UIManager.StopWait(loadingWaitGroup);
            }

            return;
        }

        // Pop last group
        RequestGroup group = PopRequestGroup();
        if (group == null)
        {
            return;
        }

        // Apply new request data
        m_RequestData = group.datas;

        m_CustomExecute = true;
        switch (batch.type)
        {
            case RequestType.Initialize:
                ExecuteInitialize();
                break;
            case RequestType.Refresh:
                ExecuteRefresh();
                break;
            case RequestType.Command:
                ExecuteCommand();
                break;
            case RequestType.ShowBegin:
                ExecuteShowBegin();
                break;
            case RequestType.ShowEnd:
                ExecuteShowEnd();
                break;
            case RequestType.HideBegin:
                ExecuteHideBegin();
                break;
            case RequestType.HideEnd:
                ExecuteHideEnd();
                break;
            case RequestType.Search:
                ExecuteSearch();
                break;
            default:
                m_RequestData = null;
                break;
        }
        m_CustomExecute = false;
    }

    //---------------------------------------------------------------------
    private void ExecuteInitialize()
    {
        OnInitialized();
        if (InitializedEvent != null)
        {
            InitializedEvent();
        }
        m_IsInitialized = true;
        UIManager.TouchWindowInitializedEvent(this);

        m_DataSources = GetComponentsInChildren<UIDataSource>(true);
        RefreshDataBinders(UIDataRefreshRate.WindowInitialize);
    }

    //---------------------------------------------------------------------
    private void ExecuteRefresh()
    {
        UIManager.TouchWindowRefreshEvent(this);
        OnRefresh();
        if (RefreshEvent != null)
        {
            RefreshEvent();
        }

        RefreshDataBinders(UIDataRefreshRate.WindowRefresh);
    }

    //---------------------------------------------------------------------
    private void ExecuteCommand()
    {
        UIManager.TouchWindowCommandEvent(this);
        OnCommand();
        if (CommandEvent != null)
        {
            CommandEvent();
        }
    }

    //---------------------------------------------------------------------
    private void ExecuteShowBegin()
    {
        m_FirstEnterShow = false;
        gameObject.SetActive(true);

        UIManager.TouchWindowPreShowEvent(this);
        OnPreShow();
        if (PreShowEvent != null)
        {
            PreShowEvent();
        }

        RefreshDataBinders(UIDataRefreshRate.WindowShow);

        if (m_CustomExecute)
        {
            if (!PlayWindowAnimation(RequestType.ShowEnd, true))
            {
                PushRequest(RequestType.ShowEnd, requestDataArray);
            }
        }

        if (UIManager.ContainsMutexGroup(windowGroup))
        {
            UIManager.HideGroup(windowGroup, GetType());
        }
    }

    //---------------------------------------------------------------------
    private void ExecuteShowEnd()
    {
        UIManager.TouchWindowPostShowEvent(this);
        OnPostShow();
        if (PostShowEvent != null)
        {
            PostShowEvent();
        }
    }

    //---------------------------------------------------------------------
    private void ExecuteHideBegin()
    {
        UIManager.TouchWindowPreHideEvent(this);
        OnPreHide();
        if (PreHideEvent != null)
        {
            PreHideEvent();
        }

        // iTween toggle message
        if (m_CustomExecute)
        {
            if (!PlayWindowAnimation(RequestType.HideEnd, false))
            {
                PushRequest(RequestType.HideEnd, requestDataArray);
            }
        }
    }

    //---------------------------------------------------------------------
    private void ExecuteHideEnd()
    {
        gameObject.SetActive(false);
        UIManager.TouchWindowPostHideEvent(this);
        OnPostHide();
        StopWait();
        if (PostHideEvent != null)
        {
            PostHideEvent();
        }

        RefreshDataBinders(UIDataRefreshRate.WindowHide);

        if (shutdownMode == UIShutdownMode.Hide &&
            !UIManager.IsRollback(GetType()))
        {
            Shutdown();
        }
    }

    //---------------------------------------------------------------------
    private void ExecuteSearch()
    {
        SearchInfo info = (SearchInfo)requestData;
        Transform result = DoSearch(info.conditionType, info.conditionValue);
        if (info.handler != null)
        {
            info.handler(result);
        }
    }

    //---------------------------------------------------------------------
    private bool PlayWindowAnimation(RequestType nextRequest, bool isShow)
    {
        RequestBatch batch = LastRequestBatch();
        if (batch != null)
        {
            // Only one animation playing.
            if (batch.type == RequestType.ShowAnimation ||
                batch.type == RequestType.HideAnimation)
            {
                return false;
            }
        }

        RequestType requestType = isShow ?
            RequestType.ShowAnimation : RequestType.HideAnimation;

        int totalAnimationCount = 0;
        AnimationOrTween.Trigger conditionTrigger = isShow ?
            AnimationOrTween.Trigger.OnActivateTrue :
            AnimationOrTween.Trigger.OnActivateFalse;
        for (int index = 0; index < m_WindowAnimations.Length; ++index)
        {
            UIPlayTween playTween = m_WindowAnimations[index];
            if (playTween.trigger != conditionTrigger ||
                !playTween.enabled || !playTween.gameObject.activeSelf ||
                playTween.tweenTarget == null)
            {
                continue;
            }

            UITweener[] childTweens = null;
            if (playTween.includeChildren)
            {
                childTweens = playTween.tweenTarget.GetComponentsInChildren<UITweener>();
            }
            else
            {
                childTweens = playTween.tweenTarget.GetComponents<UITweener>();
            }

            int ownTweenerCount = 0;
            for (int i = 0; i < childTweens.Length; ++i)
            {
                if (childTweens[i].tweenGroup == playTween.tweenGroup)
                {
                    ownTweenerCount++;
                }
            }

            if (ownTweenerCount == 0)
            {
                continue;
            }

            ++totalAnimationCount;
            EventDelegate finishedCallback = null;
            object[] lastDataArray = null;
            if (requestDataArray != null && requestDataArray.Length != 0)
            {
                lastDataArray = new object[requestDataArray.Length];
                Array.Copy(requestDataArray, lastDataArray, lastDataArray.Length);
            }

            finishedCallback = new EventDelegate(delegate()
            {
                playTween.onFinished.Remove(finishedCallback);
                --totalAnimationCount;
                if (totalAnimationCount <= 0)
                {
                    RemoveRequestBatch(requestType);
                    PushRequest(nextRequest, lastDataArray);
                }
            });
            playTween.onFinished.Add(finishedCallback);
        }

        if (totalAnimationCount == 0)
        {
            return false;
        }

        PushRequest(requestType, requestDataArray);

        // iTween toggle message
        gameObject.SendMessage("OnActivate", isShow,
            SendMessageOptions.DontRequireReceiver);

        return true;
    }

    //---------------------------------------------------------------------
    public int GetPanelDepth(UIWindowGroup group)
    {
        return 10 * (int)group;
    }

    //---------------------------------------------------------------------
    protected void DefaultClose(UIEventDispatcher sender, GameObject param)
    {
        Hide(param);
    }

    //---------------------------------------------------------------------
    protected void DefaultShowHide(UIEventDispatcher sender, string showWinClass)
    {
        Type showType = RuntimeHelper.GetType(
            "UserInterface.Windows." + showWinClass);
        if (showType == null)
        {
            DebugHelper.LogError(
                "Can not retrieve window class type: " +
                showWinClass);
            Hide();
        }
        else
        {
            UIManager.ShowHide(showType, GetType());
        }
    }

    //---------------------------------------------------------------------
    protected void DefaultRollback(UIEventDispatcher sender, GameObject param)
    {
        Hide();
        //UIManager.Rollback(GetType());
    }

    //---------------------------------------------------------------------
    private int CorrectDepth(int depth, int minDepth, int maxDepth, bool warning)
    {
        if (warning && (depth < minDepth || depth > maxDepth))
        {
            if (!UIManager.ContainsMutexGroup(windowGroup))
            {
                DebugHelper.LogWarning(GetType().Name +
                    " depth must be between " + minDepth.ToString() +
                    " ~ " + maxDepth.ToString() + ". " +
                    "The system will automatically corrected depth.");
            }
        }

        int offset = maxDepth - minDepth + 1;
        return minDepth + depth % offset;
    }

    //---------------------------------------------------------------------
    private void CorrectPanelsDepth(UIPanel[] uiPanels, int minDepth, int maxDepth)
    {
        for (int index = 0; index < uiPanels.Length; ++index)
        {
            UIPanel uiPanel = uiPanels[index];
            uiPanel.depth = CorrectDepth(
                uiPanel.depth, minDepth, maxDepth, true);
        }
    }
    #endregion

    #region Unity Method
    // Don't allow sub class use follow unity method
    //---------------------------------------------------------------------
    protected void Awake()
    {
        m_QueueLoader = new AssetQueueLoader();

        UIPanel[] uiPanels = GetComponentsInChildren<UIPanel>(true);
        if (uiPanels == null || uiPanels.Length == 0)
        {
            DebugHelper.LogException(new MissingComponentException(
                "UIWindow must be contain at least one UIPanel."));
        }

        int minDepth = GetPanelDepth(windowGroup);
        int maxDepth = GetPanelDepth((UIWindowGroup)(windowGroup + 1)) - 1;

        CorrectPanelsDepth(uiPanels, minDepth, maxDepth);
        m_QueueLoader.ImmeLoadedEvent += delegate(AssetEventArgs args)
        {
            if (args.results == null || args.results.Length == 0)
            {
                return;
            }

            for (int index = 0; index < args.results.Length; ++index)
            {
                UnityEngine.Object result = args.results[index];
                if (result == null)
                {
                    continue;
                }

                GameObject go = result as GameObject;
                if (go == null)
                {
                    continue;
                }

                UIPanel[] subPanels = go.GetComponentsInChildren<UIPanel>(true);
                CorrectPanelsDepth(subPanels, minDepth, maxDepth);
            }
        };

        m_WindowAnimations = GetComponents<UIPlayTween>();

        UIManager.TouchWindowLoaded(this);
    }

    //---------------------------------------------------------------------
    protected void Start()
    {
        gameObject.SetActive(false);

        // Make sure all widget initialized to panel.
        OnPrepared();
        if (PreparedEvent != null)
        {
            PreparedEvent();
        }

        UIManager.TouchWindowPreparedEvent(this);

        PushRequest(RequestType.Initialize, requestDataArray);
    }

    //---------------------------------------------------------------------
    protected void OnEnable()
    {
        if (!m_CustomExecute)
        {
            ExecuteShowBegin();
            ExecuteShowEnd();
        }
    }

    //---------------------------------------------------------------------
    protected void OnDisable()
    {
        if (!m_CustomExecute)
        {
            ExecuteHideBegin();
            ExecuteHideEnd();
        }
    }

    //---------------------------------------------------------------------
    protected void Update()
    {
        OnUpdate();
        if (UpdateEvent != null)
        {
            UpdateEvent();
        }
    }

    //---------------------------------------------------------------------
    protected void LateUpdate()
    {
        OnLateUpdate();
        if (LateUpdateEvent != null)
        {
            LateUpdateEvent();
        }
    }
    #endregion

    #region Internal Declare
    //---------------------------------------------------------------------
    private enum RequestType
    {
        None = 0,
        Initialize,
        ShowAnimation,
        HideAnimation,
        Load,
        Refresh,
        ShowEnd,
        HideEnd,
        ShowBegin,
        HideBegin,
        Command,
        Search,
    }

    //---------------------------------------------------------------------
    [System.Serializable]
    private class RequestGroup
    {
        public string group;
        public object[] datas;

        //---------------------------------------------------------------------
        public RequestGroup(string group, object[] datas)
        {
            this.group = group;
            this.datas = datas;
        }
    }

    //---------------------------------------------------------------------
    [System.Serializable]
    private class RequestBatch
    {
        public RequestType type = RequestType.None;
        public List<RequestGroup> groups = new List<RequestGroup>();

        //---------------------------------------------------------------------
        public RequestBatch(RequestType type)
        {
            this.type = type;
        }
    }

    //---------------------------------------------------------------------
    [System.Serializable]
    private struct SearchInfo
    {
        public string conditionType;
        public object conditionValue;
        public Param1Handler<Transform> handler;

        public SearchInfo(string type, object value,
            Param1Handler<Transform> handler)
        {
            this.conditionType = type;
            this.conditionValue = value;
            this.handler = handler;
        }
    }
    #endregion

    #region Internal Member
    //---------------------------------------------------------------------
    private UIPlayTween[] m_WindowAnimations = null;

    //---------------------------------------------------------------------
    private UIDataSource[] m_DataSources = null;

    //---------------------------------------------------------------------
    private bool m_FirstEnterShow = true;

    //---------------------------------------------------------------------
    private bool m_IsInitialized = false;

    //---------------------------------------------------------------------
    private AssetQueueLoader m_QueueLoader = null;

    //---------------------------------------------------------------------
    private object[] m_RequestData = null;

    //---------------------------------------------------------------------
    private bool m_CustomExecute = true;

    //---------------------------------------------------------------------
    private List<RequestBatch> m_RequestBatchs = new List<RequestBatch>(8);

    //---------------------------------------------------------------------
    public const string defaultGroup = "DefaultGroup";
    private const string searchGroup = "__Search_Group_";
    #endregion
}
