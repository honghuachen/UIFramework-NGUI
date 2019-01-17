using System;
using System.Collections.Generic;
using UnityEngine;

    //=========================================================================
public enum UIDataRefreshRate
{
    OnlyEditor,
    WindowInitialize,
    WindowRefresh,
    WindowShow,
    WindowHide,
    EachFrame
}

    //=========================================================================
public abstract class UIDataSource : LuaComponent
{
    #region Internal Method
    //---------------------------------------------------------------------
    internal void AddDataBinder(UIDataBinder dataBinder)
    {
        if (!m_DataBinderList.Contains(dataBinder))
        {
            m_DataBinderList.Add(dataBinder);
        }
    }

    //---------------------------------------------------------------------
    internal void RemoveDataBinder(UIDataBinder dataBinder)
    {
        m_DataBinderList.Remove(dataBinder);
    }

    //---------------------------------------------------------------------
    public virtual void RefreshDataBinders(UIDataRefreshRate refreshType)
    {
        for (int index = 0; index < m_DataBinderList.Count; ++index)
        {
            UIDataBinder dataBinder = m_DataBinderList[index];
            if (dataBinder.refreshRate == refreshType)
            {
                m_DataBinderList[index].Refresh();
            }
        }

        OnRefresh(refreshType);
    }

    //---------------------------------------------------------------------
    protected virtual void OnRefresh(UIDataRefreshRate refreshType)
    {

    }
    #endregion

    #region Internal Member
    //---------------------------------------------------------------------
    private List<UIDataBinder> m_DataBinderList = new List<UIDataBinder>();
    #endregion
}
