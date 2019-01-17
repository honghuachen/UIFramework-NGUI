using UnityEngine;
using System.Collections.Generic;

//=========================================================================
[System.Serializable]
public struct Point
{
    public int x;
    public int y;

    //---------------------------------------------------------------------
    public Point(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    //---------------------------------------------------------------------
    public override bool Equals(object obj)
    {
        return this == (Point)obj;
    }

    //---------------------------------------------------------------------
    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    //---------------------------------------------------------------------
    public override string ToString()
    {
        return "x=" + x.ToString() + ", y=" + y.ToString();
    }

    //---------------------------------------------------------------------
    public static bool operator ==(Point p1, Point p2)
    {
        return (p1.x == p2.x && p1.y == p2.y);
    }

    //---------------------------------------------------------------------
    public static bool operator !=(Point p1, Point p2)
    {
        return (p1.x != p2.x || p1.y != p2.y);
    }
}

    #region ListView SubComponent
//=========================================================================
public delegate bool UIDataBindHandler(UIListView sender, GameObject go, int index);

    //=========================================================================
[HideInInspector]
public class UIListViewItem : MonoBehaviour
{
    //---------------------------------------------------------------------
    [System.NonSerialized]
    public int itemIndex = -1;

    /// <summary>
    /// This list view control will be notify selection message by the script.
    /// </summary>
    //---------------------------------------------------------------------
    [System.NonSerialized]
    public UIListView listView = null;

    //---------------------------------------------------------------------
    public virtual bool BindData(GameObject go, int index)
    {
        return true;
    }

    //---------------------------------------------------------------------
    public virtual void Initialize(UIListView listView, GameObject go)
    {

    }

    //---------------------------------------------------------------------
    public virtual void NotifySelectItem()
    {
        if (listView != null)
        {
            listView.NotifySelectItem(this);
        }
    }
}

    //=========================================================================
//public class UIListViewSplitBar : UIListViewItem
//{
//    //---------------------------------------------------------------------
//    public Vector2 size = Vector2.zero;
//}

    //=========================================================================
public class UIListViewSelectable : MonoBehaviour
{
    /// <summary>
    /// Whether to allow selected
    /// </summary>
    //---------------------------------------------------------------------
    public bool exclude = true;

    /// <summary>
    /// This panel's contents will be dragged by the script.
    /// </summary>
    //---------------------------------------------------------------------
    internal UIListViewItem listViewItem = null;

    /// <summary>
    /// Automatically find the draggable panel if possible.
    /// </summary>
    //---------------------------------------------------------------------
    protected void Start()
    {
        if (listViewItem == null)
        {
            listViewItem = NGUITools.FindInParents<UIListViewItem>(transform);
        }
    }

    //---------------------------------------------------------------------
    private void OnClick()
    {
        if (listViewItem != null && !exclude)
        {
            listViewItem.NotifySelectItem();
        }
    }
}

    //=========================================================================
[ExecuteInEditMode]
public class UIListViewDraggable : MonoBehaviour
{
    /// <summary>
    /// Whether to allow drag
    /// </summary>
    //---------------------------------------------------------------------
    public bool exclude = true;

    /// <summary>
    /// This panel's contents will be dragged by the script.
    /// </summary>
    //---------------------------------------------------------------------
    internal UIListViewContainer listViewContainer = null;

    /// <summary>
    /// Automatically find the draggable panel if possible.
    /// </summary>
    //---------------------------------------------------------------------
    protected void Start()
    {
        if (listViewContainer == null)
        {
            listViewContainer = NGUITools.FindInParents<UIListViewContainer>(gameObject);
        }
    }

    /// <summary>
    /// Create a plane on which we will be performing the dragging.
    /// </summary>
    //---------------------------------------------------------------------
    private void OnPress(bool pressed)
    {
        if (!exclude && enabled &&
            NGUITools.GetActive(gameObject) &&
            listViewContainer != null)
        {
            listViewContainer.Press(pressed);
        }
    }

    /// <summary>
    /// Drag the object along the plane.
    /// </summary>
    //---------------------------------------------------------------------
    private void OnDrag(Vector2 delta)
    {
        if (!exclude && enabled &&
            NGUITools.GetActive(gameObject) &&
            listViewContainer != null)
        {
            listViewContainer.Drag();
        }
    }

    /// <summary>
    /// If the object should support the scroll wheel, do it.
    /// </summary>
    //---------------------------------------------------------------------
    private void OnScroll(float delta)
    {
        if (!exclude && enabled &&
            NGUITools.GetActive(gameObject) &&
            listViewContainer != null)
        {
            listViewContainer.Scroll(delta);
        }
    }
}

    //=========================================================================
[HideInInspector]
internal class UIListViewContainer : UIScrollView
{
    /// <summary>
    /// The minimum number of drag.
    /// </summary>
    //---------------------------------------------------------------------
    [SerializeField, HideInInspector]
    public UIListView listView = null;

    //---------------------------------------------------------------------
    [SerializeField, HideInInspector]
    public Vector3 initPanelPos = Vector3.zero;

    //---------------------------------------------------------------------
    public override void UpdateScrollbars(bool recalculateBounds)
    {
        base.UpdateScrollbars(recalculateBounds);

        listView.OnUpdateScrollbars();
    }

    //---------------------------------------------------------------------
    public override void Press(bool pressed)
    {
        if (listView == null)
        {
            base.Press(pressed);

            return;
        }

        if (pressed)
        {
            base.Press(pressed);
            m_StartPos = transform.localPosition;
            return;
        }

        if (!restrictWithinPanel ||
            panel.clipping == UIDrawCall.Clipping.None ||
            dragEffect != DragEffect.MomentumAndSpring ||
            (m_StartPos - transform.localPosition).sqrMagnitude < 1.0f)
        {
            return;
        }

        Vector3 distanceOffset = Vector3.zero;
        if (listView.arrangement == UIGrid.Arrangement.Horizontal)
        {
            float realWidth = listView.cellSize.x + listView.cellSpacing.x;
            float minPos = initPanelPos.x - (listView.GetTotalWidth() - (panel.baseClipRegion.z -
                (listView.marginSoftness ? listView.clipSoftness.x : 0f) * 2f));
            distanceOffset.x = CalculateOffset(realWidth,
                listView.cellCount.x,
                listView.cellCount.y,
                minPos,
                initPanelPos.x,
                transform.localPosition.x,
                m_StartPos.x);
        }
        else if (listView.arrangement == UIGrid.Arrangement.Vertical)
        {
            float realHeight = listView.cellSize.y + listView.cellSpacing.y;
            float minPos = initPanelPos.y - (listView.GetTotalHeight() - (panel.baseClipRegion.w -
                (listView.marginSoftness ? listView.clipSoftness.y : 0f) * 2f));
            distanceOffset.y = -CalculateOffset(realHeight,
                listView.cellCount.y,
                listView.cellCount.x,
                minPos,
                -initPanelPos.y,
                -transform.localPosition.y,
                -m_StartPos.y);
        }

        if (distanceOffset.sqrMagnitude > 1.0f)
        {
            // Spring back into place
            Vector3 pos = transform.localPosition;
            pos.x = Mathf.Round(pos.x + distanceOffset.x);
            pos.y = Mathf.Round(pos.y + distanceOffset.y);
            SpringPanel.Begin(panel.gameObject, pos, listView.dragSpring);
        }

        if (onDragFinished != null) onDragFinished();
    }

    //---------------------------------------------------------------------
    protected float CalculateOffset(float realSize, float xCount, float yCount,
        float minPos, float maxPos, float currentPos, float lastPos)
    {
        // Drag right direction
        if (currentPos > maxPos)
        {
            return maxPos - currentPos;
        }
        else if (currentPos < minPos)
        {
            return minPos - currentPos;
        }
        else if (listView.dragUnit == 0.0f)
        {
            if (currentPos > lastPos)
            {
                float tempPos = currentPos + listView.dragMomentum;
                if (tempPos > maxPos)
                {
                    return maxPos - currentPos;
                }

                return listView.dragMomentum;
            }
            else
            {
                float tempPos = currentPos - listView.dragMomentum;
                if (tempPos < minPos)
                {
                    return minPos - currentPos;
                }

                return -listView.dragMomentum;
            }
        }

        int evaluateCurrentUnit = Mathf.FloorToInt((maxPos - currentPos) / realSize);
        float currentPosOffset = GetOffsetByIndex(evaluateCurrentUnit, yCount);
        float offsetWithoutSplitbar = maxPos - currentPos - currentPosOffset;
        int currentIndex = Mathf.FloorToInt((offsetWithoutSplitbar / realSize) * yCount);

        // Drag right direction
        if (currentPos > lastPos)
        {
            float unitCount = Mathf.Ceil((float)currentIndex / yCount) - listView.dragUnit;
            if (yCount == 1)
                unitCount += yCount;

            currentPosOffset = GetOffsetByIndex((int)unitCount, yCount);
            float adjustPos = maxPos - (unitCount * realSize + currentPosOffset);
            return Mathf.Min(maxPos, adjustPos) - currentPos;
        }
        else
        {
            float unitCount = Mathf.Ceil((float)currentIndex / yCount) + listView.dragUnit;
            currentPosOffset = GetOffsetByIndex((int)unitCount, yCount);
            float adjustPos = maxPos - (unitCount * realSize + currentPosOffset);
            return Mathf.Max(minPos, adjustPos) - currentPos;
        }
    }

    //---------------------------------------------------------------------
    protected float GetOffsetByIndex(int index, float yCount)
    {
        int indexOffset = 0;
        float currentPosOffset = 0f;
        for (int i = 0; i < listView.m_SplitBars.Count; ++i)
        {
            UIListViewSplitBar splitBar = listView.m_SplitBars[i];
            if (splitBar == null || splitBar.itemIndex == -1)
            {
                continue;
            }

            int splitBarUnit = Mathf.CeilToInt(
                (float)(splitBar.itemIndex + indexOffset) / yCount);
            if (splitBarUnit <= index)
            {
                if (listView.arrangement == UIGrid.Arrangement.Horizontal)
                {
                    currentPosOffset += splitBar.size.x + listView.cellSpacing.x;
                }
                else if (listView.arrangement == UIGrid.Arrangement.Vertical)
                {
                    currentPosOffset += splitBar.size.y + listView.cellSpacing.y;
                }
            }

            int colIdx = (splitBar.itemIndex + indexOffset) % (int)yCount;
            indexOffset += (colIdx == 0) ? 0 : (int)yCount - colIdx;
        }

        return currentPosOffset;
    }

    //---------------------------------------------------------------------
    protected Vector3 m_StartPos = Vector3.zero;
}
    #endregion

    //=========================================================================
[ExecuteInEditMode]
public class UIListView : MonoBehaviour
{
    #region Public Setting

    #region General Setting
    //---------------------------------------------------------------------
    [SerializeField]
    protected int m_Count = 0;
    #endregion

    #region Display Setting
    /// <summary>
    /// Display item view template resource
    /// </summary>
    //---------------------------------------------------------------------
    public GameObject itemTemplate = null;

    /// <summary>
    /// Display split bar resource array
    /// </summary>
    //---------------------------------------------------------------------
    public UIListViewSplitBar[] splitBars = new UIListViewSplitBar[] { };

    /// <summary>
    /// Depth of sub panel
    /// </summary>
    //---------------------------------------------------------------------
    public int depth = 0;

    /// <summary>
    /// Drag direction
    /// </summary>
    //---------------------------------------------------------------------
    public UIGrid.Arrangement arrangement = UIGrid.Arrangement.Horizontal;

    /// <summary>
    /// The count of row and column
    /// </summary>
    //---------------------------------------------------------------------
    public Point cellCount = new Point(2, 2);

    /// <summary>
    /// The size of each of the cells.
    /// </summary>
    //---------------------------------------------------------------------
    public Vector2 cellSize = Vector2.zero;

    /// <summary>
    /// Spacing of row and column
    /// </summary>
    //---------------------------------------------------------------------
    public Vector2 cellSpacing = Vector2.zero;

    /// <summary>
    /// Custom view size
    /// </summary>
    //---------------------------------------------------------------------
    public Rect customViewport = new Rect();

    /// <summary>
    /// Clipping softness is used if the clipped style is set to "Soft".
    /// </summary>
    //---------------------------------------------------------------------
    public Vector2 clipSoftness = Vector2.zero;

    /// <summary>
    /// 
    /// </summary>
    //---------------------------------------------------------------------
    public bool marginSoftness = false;

    /// <summary>
    /// The strength of the spring of select.
    /// </summary>
    //---------------------------------------------------------------------
    public float selectSpring = 0f;
    #endregion

    #region Drag Setting
    /// <summary>
    /// Effect to apply when dragging.
    /// </summary>
    //---------------------------------------------------------------------
    public UIScrollView.DragEffect dragEffect =
        UIScrollView.DragEffect.MomentumAndSpring;

    public bool dragDisableIfFits = false;

    /// <summary>
    /// How much momentum gets applied when the press is released after dragging.
    /// </summary>
    //---------------------------------------------------------------------
    public float dragMomentum = 35f;

    /// <summary>
    /// The minimum number of drag.
    /// </summary>
    //---------------------------------------------------------------------
    public int dragUnit = 0;

    /// <summary>
    /// Scale value applied to the drag delta. 
    /// Set X or Y to 0 to disallow dragging in that direction.
    /// </summary>
    //---------------------------------------------------------------------
    public float dragScale = 1.0f;

    /// <summary>
    /// The strength of the spring.
    /// </summary>
    //---------------------------------------------------------------------
    public float dragSpring = 8f;

    #endregion

    #region Scroll Bar
    /// <summary>
    /// Condition that must be met for the scroll bars to become visible.
    /// </summary>
    //---------------------------------------------------------------------
    public UIScrollView.ShowCondition scrollBarsTrigger =
        UIScrollView.ShowCondition.OnlyIfNeeded;

    /// <summary>
    /// Horizontal/Vertical scrollbar used for visualization.
    /// </summary>
    //---------------------------------------------------------------------
    public UIScrollBar scrollBar;

    /// <summary>
    /// Effect the scroll wheel will have on the momentum.
    /// </summary>
    //---------------------------------------------------------------------
    public float scrollWheelFactor = 0f;
    #endregion

    #endregion

    #region Public Property
    /// <summary>
    /// Current item count or setting total item count
    /// </summary>
    //---------------------------------------------------------------------
    public int Count
    {
        get { return m_Count; }
        set { Refresh(m_LastBeginIndex, value); }
    }

    /// <summary>
    /// Get / Set selection item index
    /// </summary>
    //---------------------------------------------------------------------
    public int SelectIndex
    {
        get { return m_SelectIndex; }
        set
        {
            if (value == m_SelectIndex)
            {
                return;
            }

            ImmediateLocation(value);

            ItemObjectInfo itemInfo = RetrieveItemObject(value);
            if (itemInfo.item != null)
            {
                NotifySelectItem(itemInfo.item);
            }
        }
    }

    /// <summary>
    /// Get current first display item index
    /// </summary>
    //---------------------------------------------------------------------
    public int BeginIndex
    {
        get { return m_LastBeginIndex; }
        set { Refresh(value, m_Count); }
    }
    #endregion

    #region Public Event
    /// <summary>
    /// Event callback to trigger when the item should display. 
    /// </summary>
    //---------------------------------------------------------------------
    public UIDataBindHandler dataBinder;

    /// <summary>
    /// Event callback to trigger when the select item. 
    /// </summary>
    //---------------------------------------------------------------------
    public event Handler<GameObject> initItemEvent;

    /// <summary>
    /// Event callback to trigger when the select item. 
    /// </summary>
    //---------------------------------------------------------------------
    public event Handler<GameObject, int> selectItemEvent;

    /// <summary>
    /// Event callback to trigger when the drag process finished. 
    /// Can be used for additional effects, such as centering on some object.
    /// </summary>
    //---------------------------------------------------------------------
    public event Handler dragFinishedEvent;

    /// <summary>
    /// Event callback to trigger when the scroll list item. 
    /// </summary>
    //---------------------------------------------------------------------
    public event Handler<int, float> scrollEvent;

    /// <summary>
    /// Event callback to trigger when the split bar shown.
    /// </summary>
    //---------------------------------------------------------------------
    public event Handler<UIListViewSplitBar> onSplitBarShow;

    #endregion

    #region Public Method
    //---------------------------------------------------------------------
    public void Refresh()
    {
        Refresh(BeginIndex, Count);
    }

    //---------------------------------------------------------------------
    public void Refresh(int beginIndex, int newCount, params int[] splitBarIndexs)
    {
        // Update split bar index
        m_SplitBars.Clear();
        for (int i = 0; i < splitBars.Length; ++i)
        {
            UIListViewSplitBar splitBar = splitBars[i];
            // Disable all first, It will enabled at follow reposition.
            CoreUtility.SetActive(splitBar.gameObject, false);

            if (splitBar != null && i < splitBarIndexs.Length)
            {
                int newIndex = splitBarIndexs[i];
                if (newIndex < 0)
                {
                    continue;
                }

                if (newIndex >= newCount)
                {
                    Debug.LogWarning(
                        "Split bar index was out of range: " +
                        newIndex.ToString());
                    continue;
                }

                splitBar.itemIndex = newIndex;
                m_SplitBars.Add(splitBar);
            }
            else
            {
                splitBar.itemIndex = -1;
            }
        }

        m_SplitBars.Sort(delegate(UIListViewSplitBar b1, UIListViewSplitBar b2)
        {
            return b1.itemIndex - b2.itemIndex;
        });

        if (m_LastBeginIndex == -1)
        {
            Initialize();
        }

        int lastBeinIndex = m_LastBeginIndex;
        m_Count = System.Math.Max(0, newCount);

        m_LastBeginIndex = AdjustBeginIndex(beginIndex);

        int reposCount = arrangement == UIGrid.Arrangement.Horizontal ?
            cellCount.y * (cellCount.x + 2) : cellCount.x * (cellCount.y + 2);
        for (int index = 0; index < reposCount; ++index)
        {
            RefreshItemObjectData(m_ItemObjects[index], m_LastBeginIndex + index);
        }

        RefreshSplitBarStateOfActivation();
        RefreshViewBoxBackgroundSize();

        if (lastBeinIndex == m_LastBeginIndex)
        {
            return;
        }

        m_ListViewContainer.DisableSpring();

        Vector3 panelPos = m_ListViewContainer.initPanelPos;
        if (arrangement == UIGrid.Arrangement.Horizontal)
        {
            panelPos.x -= CalculateItemPosition(m_LastBeginIndex).x;
            panelPos.x = Mathf.Round(panelPos.x);
        }
        else if (arrangement == UIGrid.Arrangement.Vertical)
        {
            panelPos.y -= CalculateItemPosition(m_LastBeginIndex).y;
            panelPos.y = Mathf.Round(panelPos.y);
        }

        if (selectSpring <= 0f)
        {
            m_ListViewContainer.MoveRelative(panelPos -
                m_ListViewContainer.panel.transform.localPosition);
        }
        else
        {
            // Spring back into place
            SpringPanel.Begin(m_ListViewContainer.panel.gameObject, panelPos, selectSpring);
        }
    }

    //---------------------------------------------------------------------
    public UIListViewItem GetItemComponent(GameObject go)
    {
        if (go == null)
        {
            return null;
        }

        for (int index = 0; index < m_ItemObjects.Count; ++index)
        {
            ItemObjectInfo itemInfo = m_ItemObjects[index];
            if (itemInfo.item == null)
            {
                continue;
            }

            if (itemInfo.item.gameObject == go)
            {
                return itemInfo.item;
            }
        }

        return null;
    }

    //---------------------------------------------------------------------
    public UIListViewItem GetItemComponent(int index)
    {
        if (index < 0 || index >= Count)
        {
            return null;
        }

        return RetrieveItemObject(index).item;
    }

    //---------------------------------------------------------------------
    public GameObject GetItem(int index)
    {
        return GetItem(index, false);
    }

    //---------------------------------------------------------------------
    public GameObject GetItem(int index, bool autoLocation)
    {
        if (index < 0 || index >= Count)
        {
            return null;
        }

        if (autoLocation)
        {
            ImmediateLocation(index);
        }

        return RetrieveItemObject(index).go;
    }
    #endregion

    #region Editor Method
    //---------------------------------------------------------------------
    public void RebuildItemsInEditor()
    {
        if (!Application.isEditor || Application.isPlaying)
        {
            return;
        }

        if (itemTemplate == null)
        {
            Debug.LogWarning("ItemTemplate is null");
            return;
        }

        int totalCount = cellCount.y * cellCount.x;
        if (totalCount <= 0)
        {
            Debug.LogWarning("cellCount is invalid.");
            return;
        }

        ResetItemsInEditor();

        GameObject itemRoot = new GameObject("ItemRoot");
        ChangeParent(gameObject, itemRoot);
        m_TemplatePool = new PoolGenerator<GameObject>();
        m_TemplatePool.BuildEvent += delegate(GameObject go)
        {
            if (go != null)
            {
                go.SetActive(true);

                UIListViewItem listViewItem = go.GetComponent<UIListViewItem>();
                if (listViewItem == null)
                {
                    listViewItem = go.AddComponent<UIListViewItem>();
                }

                listViewItem.listView = this;
                listViewItem.itemIndex = -1;

                ChangeParent(itemRoot, go);
            }
        };

        m_TemplatePool.minCapacity = 1;
        m_TemplatePool.Build(itemTemplate, totalCount, false);

        if (arrangement == UIGrid.Arrangement.Horizontal)
        {
            for (int index = 0; index < totalCount; ++index)
            {
                GameObject go = m_TemplatePool.Generate();
                int col = index % (cellCount.x + 2);
                int row = (int)Mathf.Floor((float)index / (float)(cellCount.x + 2));
                int newIdx = col * cellCount.y + row;
                go.name = "Item " + newIdx.ToString();
            }
        }
        else
        {
            for (int index = 0; index < totalCount; ++index)
            {
                GameObject go = m_TemplatePool.Generate();
                go.name = "Item " + index.ToString();
            }
        }

        RepositionListViewItems(itemRoot.transform,
            cellSize.x + cellSpacing.x,
            cellSize.y + cellSpacing.y,
            cellCount.x,
            false);
    }

    //---------------------------------------------------------------------
    public void ResetItemsInEditor()
    {
        if (!Application.isEditor || Application.isPlaying)
        {
            return;
        }

        Transform itemRoot = transform.FindChild("ItemRoot");
        if (itemRoot != null)
        {
            CoreUtility.DestroyImmediate(itemRoot.gameObject);
            itemRoot = null;
        }
    }
    #endregion

    #region Unity Method
    //---------------------------------------------------------------------
    protected void Awake()
    {
        if (Application.isEditor)
        {
            ResetItemsInEditor();
        }

        if (Application.isPlaying)
        {
            ResetItems();
            Prepare();
        }
    }

    //---------------------------------------------------------------------
    protected void Start()
    {
        if (Application.isPlaying &&
            m_LastBeginIndex == -1)
        {
            Initialize();
        }
    }

    //---------------------------------------------------------------------
    protected void OnDestroy()
    {
        dataBinder = null;
        itemTemplate = null;
        scrollBar = null;
    }

    //---------------------------------------------------------------------
    private void OnDrawGizmos()
    {
        if (Application.isPlaying || itemTemplate == null)
        {
            return;
        }

        Vector3 center = transform.position;
        Vector3 size = Vector3.zero;

        center.x -= cellSize.x * 0.5f;
        center.y += cellSize.y * 0.5f;

        Color borderColor = new Color(1f, 0f, 0f);
        Color padMarginColor = new Color(1f, 1f, 0f,0.2f);

        Gizmos.color = borderColor;
        Gizmos.matrix = transform.localToWorldMatrix;

        if (customViewport.width != 0f && customViewport.height != 0f)
        {
            // Draw border or padding
            size.x = customViewport.width;
            size.y = customViewport.height;
            center.x += customViewport.x + size.x * 0.5f;
            center.y += -customViewport.y - size.y * 0.5f;
        }
        else
        {
            // Draw border or padding
            float realWidth = cellSize.x + cellSpacing.x;
            float realHeight = cellSize.y + cellSpacing.y;
            size.x = cellCount.x * realWidth - cellSpacing.x;
            size.y = cellCount.y * realHeight - cellSpacing.y;
            center.x += size.x * 0.5f;
            center.y -= size.y * 0.5f;
        }

        Bounds bounds = NGUIMath.CalculateRelativeWidgetBounds(
                itemTemplate.transform);
        center.x += bounds.center.x;
        center.y += bounds.center.y;
        Gizmos.DrawWireCube(center, size);


        if (clipSoftness == Vector2.zero)
        {
            return;
        }

        Gizmos.color = padMarginColor;
        size.x += clipSoftness.x * 2f * (marginSoftness ? 1f : -1f);
        size.y += clipSoftness.y * 2f * (marginSoftness ? 1f : -1f);
        Gizmos.DrawWireCube(center, size);
    }
    #endregion

    #region Internal Method
    //---------------------------------------------------------------------
    protected void ResetItems()
    {
        m_ItemObjects.Clear();
        m_TemplatePool.Clear();
    }

    //---------------------------------------------------------------------
    protected virtual void Prepare()
    {
        if (itemTemplate == null)
        {
            DebugHelper.LogException(
                new System.NullReferenceException(
                    "ItemTemplate can not is null."));
        }

        float realCellWidth = cellSize.x + cellSpacing.x;
        float realCellHeight = cellSize.y + cellSpacing.y;

        // Build item root node
        //-----------------------------------------------------------------
        //Transform itemRootTrans = transform.FindChild("ItemRoot");
        //if (itemRootTrans != null)
        //    CoreUtility.DestroyImmediate(itemRootTrans.gameObject);

        GameObject itemRoot = new GameObject("ItemRoot");
        ChangeParent(gameObject, itemRoot);
        UIPanel panel = itemRoot.AddComponent<UIPanel>();

        panel.depth = depth;
        panel.cullWhileDragging = true;
        panel.showInPanelTool = true;
        panel.clipping = UIDrawCall.Clipping.SoftClip;
        panel.clipSoftness = clipSoftness;

        Vector4 clipRegion = new Vector4();

        // Reset to (0,0) point
        clipRegion.x -= cellSize.x * 0.5f;
        clipRegion.y += cellSize.y * 0.5f;

        if (customViewport.width != 0f && customViewport.height != 0f)
        {
            clipRegion.z = customViewport.width;
            clipRegion.w = customViewport.height;
            clipRegion.x += clipRegion.z * 0.5f + customViewport.x;
            clipRegion.y += -clipRegion.w * 0.5f - customViewport.y;
        }
        else
        {
            clipRegion.z = realCellWidth * ((float)cellCount.x - 1f) + cellSize.x;
            clipRegion.w = realCellHeight * ((float)cellCount.y - 1f) + cellSize.y;
            clipRegion.x += clipRegion.z * 0.5f;
            clipRegion.y += -clipRegion.w * 0.5f;
        }

        clipRegion.z += (marginSoftness ? clipSoftness.x : 0f) * 2f;
        clipRegion.w += (marginSoftness ? clipSoftness.y : 0f) * 2f;

        //Fix the clipRegion not exact
        Bounds bounds = NGUIMath.CalculateRelativeWidgetBounds(
                            itemTemplate.transform);
        clipRegion.x += bounds.center.x;
        clipRegion.y += bounds.center.y;
        panel.baseClipRegion = clipRegion;

        // Build list view drag component
        UIListViewContainer listViewContainer =
            itemRoot.AddComponent<UIListViewContainer>();
        listViewContainer.listView = this;
        listViewContainer.dragEffect = dragEffect;
        listViewContainer.restrictWithinPanel = true;
        listViewContainer.disableDragIfFits = dragDisableIfFits;
        listViewContainer.smoothDragStart = true;
        listViewContainer.iOSDragEmulation = true;
        listViewContainer.scrollWheelFactor = scrollWheelFactor;
        listViewContainer.momentumAmount = dragMomentum;
        listViewContainer.horizontalScrollBar =
            arrangement == UIGrid.Arrangement.Horizontal ? scrollBar : null;
        listViewContainer.verticalScrollBar =
            arrangement == UIGrid.Arrangement.Horizontal ? null : scrollBar;
        listViewContainer.showScrollBars = scrollBarsTrigger;
        listViewContainer.movement = UIScrollView.Movement.Custom;
        listViewContainer.customMovement = arrangement == UIGrid.Arrangement.Horizontal ?
            new Vector2(dragScale, 0f) : new Vector2(0f, dragScale);
        m_ListViewContainer = listViewContainer;
        listViewContainer.onDragFinished += OnDragFinished;

        // make sure collider exist
        Transform internalNode = new GameObject("_Internal").transform;
        CoreUtility.AttachChild(listViewContainer.transform, internalNode, true);
        CoreUtility.NormalizeTransform(internalNode);

        GameObject containnerColliderGo = new GameObject("MaskCollider");
        CoreUtility.AttachChild(internalNode, containnerColliderGo.transform, true);
        CoreUtility.NormalizeTransform(containnerColliderGo);
        Collider containerCollider = containnerColliderGo.AddComponent<BoxCollider>();
        containerCollider.isTrigger = true;

        // make sure that under all sprite
        m_ContainerColliderTexture = containnerColliderGo.AddComponent<UITexture>();
        m_ContainerColliderTexture.depth = -1;

        if (ms_BackgroundColliderTexture == null)
        {
            ms_BackgroundColliderTexture = new Texture2D(
                1, 1, TextureFormat.RGBA32, false, false);
            ms_BackgroundColliderTexture.SetPixel(0, 0, new Color(0, 0, 0, 0));
            ms_BackgroundColliderTexture.Apply();
        }
        m_ContainerColliderTexture.type = UIBasicSprite.Type.Tiled;
        m_ContainerColliderTexture.autoResizeBoxCollider = true;
        m_ContainerColliderTexture.mainTexture = ms_BackgroundColliderTexture;

        // make sure draggable exist
        if (m_ContainerColliderTexture.GetComponent<UIListViewDraggable>() == null)
        {
            UIListViewDraggable listViewDraggable =
                m_ContainerColliderTexture.gameObject.AddComponent<UIListViewDraggable>();
            listViewDraggable.exclude = false;
        }

        Collider[] colliderList = itemTemplate.GetComponentsInChildren<Collider>(true);
        foreach (Collider colliderNode in colliderList)
        {
            UIListViewDraggable draggableNode =
                colliderNode.GetComponent<UIListViewDraggable>();
            if (draggableNode == null)
            {
                draggableNode = colliderNode.gameObject.AddComponent<UIListViewDraggable>();
                draggableNode.exclude = false;
            }
            draggableNode.listViewContainer = listViewContainer;

            if (colliderNode.GetComponent<UIListViewSelectable>() == null)
            {
                UIListViewSelectable selectable =
                    colliderNode.gameObject.AddComponent<UIListViewSelectable>();
                selectable.exclude = false;
            }
        }

        // Build item pool
        m_TemplatePool.BuildEvent += delegate(GameObject go)
        {
            if (go != null)
            {
                UIListViewItem listViewItem = go.GetComponent<UIListViewItem>();
                if (listViewItem == null)
                {
                    listViewItem = go.AddComponent<UIListViewItem>();
                }

                listViewItem.listView = this;
                listViewItem.itemIndex = -1;

                ChangeParent(itemRoot, go);
            }
        };

        int showCount = 0;
        if (arrangement == UIGrid.Arrangement.Horizontal)
        {
            showCount = cellCount.y * (cellCount.x + 2);
        }
        else
        {
            showCount = (cellCount.y + 2) * cellCount.x;
        }

        // Touch activation in order to Touch the initialization of Awake
        itemTemplate.SetActive(true);

        if (!m_TemplatePool.Build(itemTemplate, showCount, false))
        {
            DebugHelper.LogException(new System.Exception(
                    "Build item template pool failed."));
        }

        // Restore item template
        itemTemplate.SetActive(false);

        // Build item object list
        if (arrangement == UIGrid.Arrangement.Horizontal)
        {
            Map<int, GameObject> tempMap = new Map<int, GameObject>();
            for (int index = 0; index < showCount; ++index)
            {
                GameObject go = m_TemplatePool.Generate();
                int col = index % (cellCount.x + 2);
                int row = (int)Mathf.Floor((float)index / (float)(cellCount.x + 2));
                int newIdx = col * cellCount.y + row;
                go.name = "Item " + newIdx.ToString();
                tempMap.Add(newIdx, go);
            }

            for (int index = 0; index < showCount; ++index)
            {
                GameObject go = tempMap[index];
                m_ItemObjects.Add(new ItemObjectInfo(go));
            }
            tempMap.Clear();
        }
        else
        {
            for (int index = 0; index < showCount; ++index)
            {
                GameObject go = m_TemplatePool.Generate();
                go.name = "Item " + index.ToString();
                m_ItemObjects.Add(new ItemObjectInfo(go));
            }
        }

        RepositionListViewItems(itemRoot.transform, realCellWidth, realCellHeight,
            arrangement == UIGrid.Arrangement.Horizontal ? cellCount.x + 2 : cellCount.x, false);

        for (int i = 0; i < splitBars.Length; ++i)
        {
            UIListViewSplitBar splitBar = splitBars[i];
            if (splitBar != null)
            {
                CoreUtility.AttachChild(internalNode, splitBar.transform);
                CoreUtility.SetActive(splitBar, false);
            }
        }
    }

    //---------------------------------------------------------------------
    protected virtual void Initialize()
    {
        UIWindow win = NGUITools.FindInParents<UIWindow>(transform);
        if (win != null)
        {
            UIPanel[] panels = GetComponentsInChildren<UIPanel>(true);
            for (int index = 0; index < panels.Length; ++index)
            {
                panels[index].depth = win.CorrectDepth(panels[index].depth);
            }
        }

        for (int index = 0; index < m_ItemObjects.Count; ++index)
        {
            GameObject go = m_ItemObjects[index].go;
            UIListViewItem listViewItem = m_ItemObjects[index].item;
            if (listViewItem != null)
            {
                listViewItem.Initialize(this, go);
            }

            if (initItemEvent != null)
            {
                initItemEvent(go);
            }

            if (index >= Count)
            {
                m_ItemObjects[index].go.SetActive(false);
            }
        }

        m_LastBeginIndex = cellCount.x * (cellCount.y + 2);
        if (arrangement == UIGrid.Arrangement.Horizontal)
        {
            m_LastBeginIndex = (cellCount.x + 2) * cellCount.y;
        }

        RefreshItemObject(0, true);
    }

    //---------------------------------------------------------------------
    protected void OnDragFinished()
    {
        if (m_ListViewContainer == null)
        {
            return;
        }

        if (dragFinishedEvent != null)
        {
            dragFinishedEvent();
        }
    }

    //---------------------------------------------------------------------
    internal void NotifySelectItem(UIListViewItem listViewItem)
    {
        if (selectItemEvent != null)
        {
            selectItemEvent(
                listViewItem.gameObject,
                listViewItem.itemIndex);
        }

        m_SelectIndex = listViewItem.itemIndex;
    }

    //---------------------------------------------------------------------
    internal int CalculateIndexByPosition(Vector3 currentPos)
    {
        int currentBeginIndex = -1;
        Vector3 initPos = m_ListViewContainer.initPanelPos;
        Vector3 currentOffset = currentPos - initPos;

        for (int index = 0; index < Count; ++index)
        {
            Vector3 tempPos = CalculateItemPosition(index);
            if (arrangement == UIGrid.Arrangement.Horizontal)
            {
                if (-currentOffset.x - tempPos.x <= cellSize.x + cellSpacing.x)
                {
                    currentBeginIndex = index;
                    break;
                }
            }
            else if (arrangement == UIGrid.Arrangement.Vertical)
            {
                if (currentOffset.y - (-tempPos.y) <= cellSize.y + cellSpacing.y)
                {
                    currentBeginIndex = index;
                    break;
                }
            }
        }

        return currentBeginIndex;
    }

    //---------------------------------------------------------------------
    internal void OnUpdateScrollbars()
    {
        if (m_ListViewContainer == null ||
            m_ListViewContainer.panel == null ||
            m_LastBeginIndex == -1)
        {
            return;
        }

        int currentBeginIndex = CalculateIndexByPosition(
            m_ListViewContainer.transform.localPosition);
        AdjustBeginIndex(currentBeginIndex);

        RefreshItemObject(currentBeginIndex, false);

        if (scrollEvent != null)
        {
            if (arrangement == UIGrid.Arrangement.Horizontal)
            {
                scrollEvent(currentBeginIndex, (m_ListViewContainer.transform.localPosition.x -
                m_ListViewContainer.initPanelPos.x) / GetTotalWidth());
            }
            else if (arrangement == UIGrid.Arrangement.Vertical)
            {
                scrollEvent(currentBeginIndex, (m_ListViewContainer.transform.localPosition.y -
                m_ListViewContainer.initPanelPos.y) / GetTotalHeight());

            }
        }
    }

    //---------------------------------------------------------------------
    protected void RefreshItemObject(int currentBeginIndex, bool isCreate)
    {
        int reposCount = 0;
        if (!isCreate)
        {
            currentBeginIndex = System.Math.Min(
                Count - cellCount.x * cellCount.y, currentBeginIndex);
            currentBeginIndex = System.Math.Max(
                0, currentBeginIndex);

            if (currentBeginIndex == m_LastBeginIndex ||
                currentBeginIndex < 0 ||
                currentBeginIndex > Count - cellCount.y * cellCount.x)
            {
                return;
            }

            reposCount = currentBeginIndex - m_LastBeginIndex;
            reposCount = System.Math.Abs(reposCount);
            reposCount = System.Math.Min(reposCount, cellCount.x * cellCount.y);
        }
        else
        {
            reposCount = currentBeginIndex - m_LastBeginIndex;
            reposCount = System.Math.Abs(reposCount);
        }

        // Right or down direction, high to low
        if (m_LastBeginIndex > currentBeginIndex)
        {
            int lastIndex = m_ItemObjects.Count - reposCount;
            List<ItemObjectInfo> tempItemList =
                m_ItemObjects.GetRange(lastIndex, reposCount);
            for (int index = lastIndex - 1; index >= 0; --index)
            {
                m_ItemObjects[reposCount + index] = m_ItemObjects[index];
            }

            for (int index = 0; index < reposCount; ++index)
            {
                m_ItemObjects[index] = tempItemList[index];
                RefreshItemObjectData(tempItemList[index], currentBeginIndex + index);
            }
        }
        else // low to high
        {
            int lastIndex = m_ItemObjects.Count - reposCount;
            List<ItemObjectInfo> tempItemList = m_ItemObjects.GetRange(0, reposCount);
            for (int index = 0; index < lastIndex; ++index)
            {
                m_ItemObjects[index] = m_ItemObjects[reposCount + index];
            }

            for (int index = 0; index < reposCount; ++index)
            {
                m_ItemObjects[lastIndex + index] = tempItemList[index];
                RefreshItemObjectData(tempItemList[index],
                    currentBeginIndex + m_ItemObjects.Count - reposCount + index);
            }
        }

        m_LastBeginIndex = currentBeginIndex;

        RefreshSplitBarStateOfActivation();
    }

    //---------------------------------------------------------------------
    protected void RefreshSplitBarStateOfActivation()
    {
        int totalShowCount = cellCount.x * cellCount.y;
        for (int i = 0; i < m_SplitBars.Count; ++i)
        {
            UIListViewSplitBar splitBar = m_SplitBars[i];
            if (splitBar == null)
            {
                continue;
            }

            if (splitBar.itemIndex == -1 ||
                splitBar.itemIndex < m_LastBeginIndex ||
                splitBar.itemIndex > m_LastBeginIndex + totalShowCount)
            {
                CoreUtility.SetActive(splitBar, false);
            }
            else
            {
                CoreUtility.SetActive(splitBar, true);
            }
        }
    }

    //---------------------------------------------------------------------
    protected Vector3 CalculateItemPosition(int index)
    {
        int indexOffset = 0;
        float xOffset = 0f;
        float yOffset = 0f;

        for (int i = 0; i < m_SplitBars.Count; ++i)
        {
            UIListViewSplitBar splitBar = m_SplitBars[i];
            if (splitBar == null ||
                splitBar.itemIndex <= -1 ||
                splitBar.itemIndex >= Count)
            {
                continue;
            }

            // Statistical offset
            if (splitBar.itemIndex <= index)
            {
                if (arrangement == UIGrid.Arrangement.Horizontal)
                {
                    int rowIdx = (splitBar.itemIndex + indexOffset) % cellCount.y;
                    indexOffset += (rowIdx == 0) ? 0 : cellCount.y - rowIdx;
                    xOffset += splitBar.size.x + cellSpacing.x;
                }
                else if (arrangement == UIGrid.Arrangement.Vertical)
                {
                    int colIdx = (splitBar.itemIndex + indexOffset) % cellCount.x;
                    indexOffset += (colIdx == 0) ? 0 : cellCount.x - colIdx;
                    yOffset += splitBar.size.y + cellSpacing.y;
                }
            }
        }

        float row = 0;
        float col = 0;
        if (arrangement == UIGrid.Arrangement.Horizontal)
        {
            row = (index + indexOffset) % cellCount.y;
            col = (int)Mathf.Floor((float)(index + indexOffset) / (float)cellCount.y);
        }
        else if (arrangement == UIGrid.Arrangement.Vertical)
        {
            row = (int)Mathf.Floor((float)(index + indexOffset) / (float)cellCount.x);
            col = (index + indexOffset) % cellCount.x;
        }

        Vector3 newPos = Vector3.zero;
        newPos.x = (cellSize.x + cellSpacing.x) * col + xOffset;
        newPos.y = -((cellSize.y + cellSpacing.y) * row + yOffset);

        return newPos;
    }

    //---------------------------------------------------------------------
    protected UIListViewSplitBar GetSplitBarByIndex(int index)
    {
        for (int i = 0; i < m_SplitBars.Count; ++i)
        {
            UIListViewSplitBar splitBar = m_SplitBars[i];
            if (splitBar != null &&
                splitBar.itemIndex == index)
            {
                return splitBar;
            }
        }

        return null;
    }

    //---------------------------------------------------------------------
    protected void RefreshItemObjectData(ItemObjectInfo itemInfo, int index)
    {
        if (index < 0 || index >= Count)
        {
            if (itemInfo.go.activeSelf)
            {
                itemInfo.go.SetActive(false);
            }
            return;
        }

        Vector3 newPos = CalculateItemPosition(index);
        itemInfo.go.transform.localPosition = newPos;

        if (itemInfo.item != null)
        {
            itemInfo.item.itemIndex = index;
        }

        // Update split bar position
        UIListViewSplitBar currentSplitBar = GetSplitBarByIndex(index);
        if (currentSplitBar != null)
        {
            if (arrangement == UIGrid.Arrangement.Horizontal)
            {
                newPos.x -= (cellSize.x + currentSplitBar.size.x) * 0.5f + cellSpacing.x;
                newPos.y = -(cellCount.y - 1) * (cellSize.y + cellSpacing.y) * 0.5f;
            }
            else if (arrangement == UIGrid.Arrangement.Vertical)
            {
                newPos.x += (cellCount.x - 1) * (cellSize.x + cellSpacing.x) * 0.5f;
                newPos.y += (cellSize.y + currentSplitBar.size.y) * 0.5f + cellSpacing.y;
            }

            currentSplitBar.transform.localPosition = newPos;

            if (onSplitBarShow != null)
            {
                onSplitBarShow(currentSplitBar);
            }
        }

        if (!itemInfo.go.activeSelf)
        {
            itemInfo.go.SetActive(true);
        }

        if (dataBinder != null)
        {
            if (!dataBinder(this, itemInfo.go, index))
            {
                Debug.LogWarning("Failed to fill data at " + index.ToString());
            }
        }

        if (itemInfo.item != null)
        {
            if (!itemInfo.item.BindData(itemInfo.go, index))
            {
                Debug.LogWarning("Failed to fill data at " + index.ToString());
            }
        }

        if (itemInfo.item == null && dataBinder == null)
        {
            DebugHelper.LogWarning("No display binder, can not fill data to item");
        }
    }

    //---------------------------------------------------------------------
    internal float GetTotalWidth()
    {
        float itemWidth = 0f;
        float widthOffset = 0f;
        float indexOffset = 0f;
        for (int i = 0; i < m_SplitBars.Count; ++i)
        {
            UIListViewSplitBar splitBar = m_SplitBars[i];
            if (splitBar == null ||
                splitBar.itemIndex <= -1 ||
                splitBar.itemIndex >= Count)
            {
                continue;
            }

            if (arrangement == UIGrid.Arrangement.Horizontal)
            {
                int rowIdx = (splitBar.itemIndex + (int)indexOffset) % cellCount.y;
                indexOffset += (rowIdx == 0) ? 0 : cellCount.y - rowIdx;
            }
            else if (arrangement == UIGrid.Arrangement.Vertical)
            {
                int colIdx = (splitBar.itemIndex + (int)indexOffset) % cellCount.x;
                indexOffset += (colIdx == 0) ? 0 : cellCount.x - colIdx;
            }
            widthOffset += splitBar.size.x + cellSpacing.x;
        }

        if (arrangement == UIGrid.Arrangement.Horizontal)
        {
            int totalColCount = Mathf.CeilToInt((float)(Count + indexOffset) / (float)cellCount.y);
            itemWidth = cellSize.x + (totalColCount - 1f) * (cellSize.x + cellSpacing.x);
        }
        else if (arrangement == UIGrid.Arrangement.Vertical)
        {
            itemWidth = cellSize.x + (cellCount.x - 1f) * (cellSize.x + cellSpacing.x);
        }

        float totalWidth = itemWidth + widthOffset;

        return Mathf.Max(totalWidth, m_ListViewContainer.panel.baseClipRegion.z);
    }

    //---------------------------------------------------------------------
    internal float GetTotalHeight()
    {
        float itemHeight = 0f;
        float heightOffset = 0f;
        float indexOffset = 0f;
        for (int i = 0; i < m_SplitBars.Count; ++i)
        {
            UIListViewSplitBar splitBar = m_SplitBars[i];
            if (splitBar == null ||
                splitBar.itemIndex <= -1 ||
                splitBar.itemIndex >= Count)
            {
                continue;
            }

            if (arrangement == UIGrid.Arrangement.Horizontal)
            {
                int rowIdx = (splitBar.itemIndex + (int)indexOffset) % cellCount.y;
                indexOffset += (rowIdx == 0) ? 0 : cellCount.y - rowIdx;
            }
            else if (arrangement == UIGrid.Arrangement.Vertical)
            {
                int colIdx = (splitBar.itemIndex + (int)indexOffset) % cellCount.x;
                indexOffset += (colIdx == 0) ? 0 : cellCount.x - colIdx;
            }
            heightOffset += splitBar.size.y + cellSpacing.y;
        }

        if (arrangement == UIGrid.Arrangement.Horizontal)
        {
            itemHeight = cellSize.y + (cellCount.y - 1f) * (cellSize.y + cellSpacing.y);
        }
        else if (arrangement == UIGrid.Arrangement.Vertical)
        {
            int totalRowCount = Mathf.CeilToInt((float)(Count + indexOffset) / (float)cellCount.x);
            itemHeight = cellSize.y + (totalRowCount - 1f) * (cellSize.y + cellSpacing.y);
        }

        float totalHeight = itemHeight + heightOffset;

        return Mathf.Max(totalHeight, m_ListViewContainer.panel.baseClipRegion.w);
    }

    //---------------------------------------------------------------------
    protected void ChangeParent(GameObject parent, GameObject go)
    {
        if (go == null || parent == null)
        {
            return;
        }

        go.transform.parent = parent.transform;
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        if (UIManager.layer == -1)
        {
            go.layer = parent.layer;
        }
        else
        {
            go.layer = UIManager.layer;
            parent.layer = UIManager.layer;
        }
    }

    //---------------------------------------------------------------------
    protected void RepositionListViewItems(Transform trans, float realWidth,
        float realHeight, int maxPerLine, bool hideInactive)
    {
        int x = 0;
        int y = 0;

        UIListViewItem[] itemArray = trans.GetComponentsInChildren<UIListViewItem>();
        for (int i = 0; i < itemArray.Length; ++i)
        {
            UIListViewItem item = itemArray[i];
            if (item == null || item.transform.parent != trans)
            {
                continue;
            }

            if (!NGUITools.GetActive(item.gameObject) && hideInactive)
            {
                continue;
            }

            float depth = item.transform.localPosition.z;
            item.transform.localPosition = new Vector3(
                realWidth * x, -realHeight * y, depth);

            if (++x >= maxPerLine && maxPerLine > 0)
            {
                x = 0;
                ++y;
            }
        }
    }

    //---------------------------------------------------------------------
    protected ItemObjectInfo RetrieveItemObject(int requestIndex)
    {
        for (int index = 0; index < m_ItemObjects.Count; ++index)
        {
            ItemObjectInfo itemInfo = m_ItemObjects[index];
            if (itemInfo.item == null)
            {
                continue;
            }

            if (itemInfo.item.itemIndex == requestIndex)
            {
                return itemInfo;
            }
        }

        return default(ItemObjectInfo);
    }

    //---------------------------------------------------------------------
    protected void ImmediateLocation(int index)
    {
        if (index > BeginIndex && index < BeginIndex + cellCount.y * cellCount.x)
        {
            return;
        }

        // Force immediate refresh
        float tempSelectSpring = selectSpring;
        selectSpring = 0.0f;
        Refresh(index, Count);
        selectSpring = tempSelectSpring;
    }

    //---------------------------------------------------------------------
    protected void RefreshViewBoxBackgroundSize()
    {
        if (arrangement == UIGrid.Arrangement.Vertical)
        {
            float totalHeight = GetTotalHeight();

            Vector3 pos = m_ContainerColliderTexture.transform.localPosition;
            pos.x = m_ListViewContainer.panel.baseClipRegion.x;
            pos.y = -(totalHeight - cellSize.y - cellSpacing.x) * 0.5f;
            m_ContainerColliderTexture.transform.localPosition = pos;

            m_ContainerColliderTexture.SetDimensions(
                (int)m_ListViewContainer.panel.baseClipRegion.z, (int)totalHeight);

            // Must be invoke by manual, because at that point the gameObject may be disabled.
            NGUITools.UpdateWidgetCollider(m_ContainerColliderTexture.gameObject);
        }
        else if (arrangement == UIGrid.Arrangement.Horizontal)
        {
            float totalWidth = GetTotalWidth();

            Vector3 pos = m_ContainerColliderTexture.transform.localPosition;
            pos.x = (totalWidth - cellSize.x - cellSpacing.y) * 0.5f;
            pos.y = m_ListViewContainer.panel.baseClipRegion.y;
            m_ContainerColliderTexture.transform.localPosition = pos;

            m_ContainerColliderTexture.SetDimensions(
                (int)totalWidth, (int)m_ListViewContainer.panel.baseClipRegion.w);

            // Must be invoke by manual, because at that point the gameObject may be disabled.
            NGUITools.UpdateWidgetCollider(m_ContainerColliderTexture.gameObject);
        }
    }

    //---------------------------------------------------------------------
    private int AdjustBeginIndex(int index)
    {
        int totalAddCount = 0;
        int currentAddCount = 0;
        for (int i = 0; i < m_SplitBars.Count; ++i)
        {
            UIListViewSplitBar splitBar = m_SplitBars[i];
            if (splitBar == null || splitBar.itemIndex <= -1)
            {
                continue;
            }

            if (arrangement == UIGrid.Arrangement.Horizontal)
            {
                int addCount = cellCount.y - (splitBar.itemIndex + totalAddCount) % cellCount.y;
                totalAddCount += addCount;
                if (splitBar.itemIndex <= index)
                {
                    currentAddCount += addCount;
                }
            }
            else if (arrangement == UIGrid.Arrangement.Vertical)
            {
                int addCount = cellCount.x - (splitBar.itemIndex + totalAddCount) % cellCount.x;
                totalAddCount += addCount;
                if (splitBar.itemIndex <= index)
                {
                    currentAddCount += addCount;
                }
            }
        }

        int remainCount = 0;
        if (arrangement == UIGrid.Arrangement.Horizontal)
        {
            if ((Count + totalAddCount) % cellCount.y != 0)
            {
                remainCount = cellCount.y - (Count + totalAddCount) % cellCount.y;
            }
        }
        else if (arrangement == UIGrid.Arrangement.Vertical)
        {
            if ((Count + totalAddCount) % cellCount.x != 0)
            {
                remainCount = cellCount.x - (Count + totalAddCount) % cellCount.x;
            }
        }

        int maxIndex = Count + totalAddCount + remainCount - cellCount.y * cellCount.x;
        if (index + currentAddCount >= maxIndex)
        {
            index = maxIndex - currentAddCount;
        }

        return Mathf.Max(0, index);
    }
    #endregion

    #region Internal Declear
    //---------------------------------------------------------------------
    protected struct ItemObjectInfo
    {
        public GameObject go;
        public UIListViewItem item;

        //-----------------------------------------------------------------
        public ItemObjectInfo(GameObject go)
        {
            this.go = go;
            this.item = go.GetComponent<UIListViewItem>();
        }
    }
    #endregion

    #region Internal Member
    //---------------------------------------------------------------------
    private static Texture2D ms_BackgroundColliderTexture = null;

    //---------------------------------------------------------------------
    [SerializeField, HideInInspector]
    protected int m_LastBeginIndex = -1;

    //---------------------------------------------------------------------
    [SerializeField, HideInInspector]
    protected int m_SelectIndex = -1;

    //---------------------------------------------------------------------
    [SerializeField, HideInInspector]
    internal UIListViewContainer m_ListViewContainer = null;

    //---------------------------------------------------------------------
    protected UITexture m_ContainerColliderTexture = null;

    //---------------------------------------------------------------------
    internal List<UIListViewSplitBar> m_SplitBars = new List<UIListViewSplitBar>();

    //---------------------------------------------------------------------
    protected List<ItemObjectInfo> m_ItemObjects = new List<ItemObjectInfo>();

    //---------------------------------------------------------------------
    protected PoolGenerator<GameObject> m_TemplatePool =
        new PoolGenerator<GameObject>();
    #endregion
}
